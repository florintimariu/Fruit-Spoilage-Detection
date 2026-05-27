using System.Net;
using System.Net.Http.Json;
using Backend.Models;
using FluentAssertions;
using Google.Cloud.Firestore;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class VerifyEndpointTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public VerifyEndpointTests(FirestoreEmulatorFixture fixture)
    {
        _fixture = fixture;
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _fixture.ClearAllCollectionsAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Verify_StepNotCompleted_ReturnsNotCompleted()
    {
        // Arrange — cream un step dar nu il completam
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var startResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new { type = "Transport", locationName = "X", operatorName = "Y" });
        var step = await startResponse.Content.ReadFromJsonAsync<Step>();

        // Act — incercam sa verificam un step necompletat
        var response = await _client.GetAsync(
            $"/api/shipments/{shipmentId}/steps/{step!.StepId}/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerificationResult>();
        result!.IsValid.Should().BeFalse();
        result.Status.Should().Be("NOT_COMPLETED");
    }

    [Fact]
    public async Task Verify_StepCompletedButNotAnchored_ReturnsNotAnchored()
    {
        // Arrange — injectam direct in Firestore un step completat dar fara TransactionHash
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Cream step-ul prin API
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var startResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new { type = "Transport", locationName = "X", operatorName = "Y" });
        var step = await startResponse.Content.ReadFromJsonAsync<Step>();

        // Marcam manual ca completat, dar FARA TransactionHash (simulam anchoring esuat)
        await _fixture.Firestore
            .Collection("Shipments").Document(shipmentId)
            .Collection("Steps").Document(step!.StepId)
            .UpdateAsync(new Dictionary<string, object>
            {
                { "IsCompleted", true },
                { "DataHash", "0xabc123" },
                { "AiStatusAtCompletion", "Fresh" }
                // TransactionHash intentionat absent
            });

        // Act
        var response = await _client.GetAsync(
            $"/api/shipments/{shipmentId}/steps/{step.StepId}/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerificationResult>();
        result!.IsValid.Should().BeFalse();
        result.Status.Should().Be("NOT_ANCHORED");
        result.StoredHash.Should().Be("0xabc123");
    }

    [Fact]
    public async Task Verify_Unauthorized_Returns401()
    {
        // Act — fara header de autentificare
        var response = await _client.GetAsync(
            "/api/shipments/any-id/steps/any-step/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Verify_StepNotFound_ReturnsForbiddenOrNotFound()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Act
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var response = await _client.GetAsync(
            $"/api/shipments/{shipmentId}/steps/non-existent-step/verify");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VerificationResult>();
        result!.IsValid.Should().BeFalse();
        result.Status.Should().Be("STEP_NOT_FOUND");
    }

    // ===== Helpers =====

    private async Task CreateTestUserAsync(string userId)
    {
        var user = new User
        {
            UserId = userId,
            Email = $"{userId}@test.com",
            DisplayName = userId,
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            OrganizationIds = new List<string>()
        };
        await _fixture.Firestore.Collection("Users").Document(userId).SetAsync(user);
    }

    private async Task<string> CreateOrgAsync(string ownerId, string name)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", ownerId);
        var response = await _client.PostAsJsonAsync("/api/organizations", new { name });
        var org = await response.Content.ReadFromJsonAsync<Organization>();
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        return org!.OrganizationId;
    }

    private async Task<string> CreateShipmentAsync(string userId, string orgId)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        var response = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "Test",
            productDescription = "",
            origin = "A",
            destination = "B"
        });
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        return shipment!.ShipmentId;
    }

    // DTO pentru raspunsul de la verify
    private record VerificationResult(
        bool IsValid,
        string Status,
        string? StoredHash,
        string? OnChainHash,
        string? TransactionHash,
        string? Message);
}