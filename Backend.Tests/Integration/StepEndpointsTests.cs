using System.Net;
using System.Net.Http.Json;
using Backend.Models;
using FluentAssertions;
using Google.Cloud.Firestore;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class StepEndpointsTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public StepEndpointsTests(FirestoreEmulatorFixture fixture)
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
    public async Task StartStep_Operator_ReturnsCreated()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Act
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new
            {
                type = "Transport",
                locationName = "Depozit Cluj",
                operatorName = "Ion Popescu"
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var step = await response.Content.ReadFromJsonAsync<Step>();
        step!.ShipmentId.Should().Be(shipmentId);
        step.Type.Should().Be("Transport");
        step.LocationName.Should().Be("Depozit Cluj");
        step.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task StartStep_Viewer_Returns403()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        await CreateTestUserAsync("user-viewer");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        await AddMemberDirectAsync(orgId, "user-viewer", "Viewer");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Act
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-viewer");
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new { type = "Transport", locationName = "X", operatorName = "Y" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetSteps_MemberCanList_ReturnsOk()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Cream 2 steps
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        await _client.PostAsJsonAsync($"/api/shipments/{shipmentId}/steps",
            new { type = "Harvest", locationName = "Ferma A", operatorName = "Op1" });
        await _client.PostAsJsonAsync($"/api/shipments/{shipmentId}/steps",
            new { type = "Warehouse", locationName = "Depozit B", operatorName = "Op2" });

        // Act
        var response = await _client.GetAsync($"/api/shipments/{shipmentId}/steps");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var steps = await response.Content.ReadFromJsonAsync<List<Step>>();
        steps.Should().HaveCount(2);
        steps!.Select(s => s.Type).Should().BeEquivalentTo(new[] { "Harvest", "Warehouse" });
    }

    [Fact]
    public async Task CompleteStep_SetsIsCompletedTrue()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var startResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new { type = "Transport", locationName = "X", operatorName = "Y" });
        var step = await startResponse.Content.ReadFromJsonAsync<Step>();

        // Act — completeaza step-ul
        var completeResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{step!.StepId}/complete",
            new { aiStatus = "Fresh" });

        // Assert
        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await completeResponse.Content.ReadFromJsonAsync<CompleteStepResponse>();
        result!.Step.IsCompleted.Should().BeTrue();
        result.Step.AiStatusAtCompletion.Should().Be("Fresh");
        // Blockchain anchoring poate esua in testing (no real Ethereum)
        // dar step-ul trebuie sa fie marcat completed oricum
    }

    [Fact]
    public async Task CompleteStep_StepNotFound_ReturnsNotFound()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        var shipmentId = await CreateShipmentAsync("user-owner", orgId);

        // Act — incercam sa completam un step care nu exista
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/non-existent-step/complete",
            new { aiStatus = "Fresh" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
            productName = "Test Shipment",
            productDescription = "",
            origin = "Cluj",
            destination = "Bucuresti"
        });
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        return shipment!.ShipmentId;
    }

    private async Task AddMemberDirectAsync(string orgId, string userId, string role)
    {
        var docRef = _fixture.Firestore.Collection("Organizations").Document(orgId);
        var snapshot = await docRef.GetSnapshotAsync();
        var org = snapshot.ConvertTo<Organization>();
        org.Members.Add(new Member
        {
            UserId = userId,
            Role = role,
            JoinedAt = Timestamp.GetCurrentTimestamp()
        });
        await docRef.SetAsync(org);

        var userRef = _fixture.Firestore.Collection("Users").Document(userId);
        var userSnap = await userRef.GetSnapshotAsync();
        var user = userSnap.ConvertTo<User>();
        if (!user.OrganizationIds.Contains(orgId))
        {
            user.OrganizationIds.Add(orgId);
            await userRef.SetAsync(user);
        }
    }

    // DTO pentru raspunsul de la complete
    private record CompleteStepResponse(
        Step Step,
        string? TransactionHash,
        bool AnchoringSucceeded,
        string? ErrorMessage);
}