using System.Net;
using System.Net.Http.Json;
using Backend.Models;
using FluentAssertions;
using Google.Cloud.Firestore;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class ShipmentEndpointsTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ShipmentEndpointsTests(FirestoreEmulatorFixture fixture)
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
    public async Task CreateShipment_Owner_ReturnsCreated()
    {
        // Arrange — user-A creeaza org si va fi Owner automat
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");

        // Act
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var response = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "Apples",
            productDescription = "10 crates",
            origin = "Cluj",
            destination = "Bucuresti"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        shipment!.ProductName.Should().Be("Apples");
        shipment.Status.Should().Be("Created");
        shipment.CreatedByUserId.Should().Be("user-owner");
    }

    [Fact]
    public async Task CreateShipment_Viewer_Returns403()
    {
        // Arrange — user-viewer e doar Viewer in organizatie
        await CreateTestUserAsync("user-owner");
        await CreateTestUserAsync("user-viewer");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");
        await AddMemberAsync(orgId, "user-viewer", "Viewer");

        // Act — user-viewer incearca sa creeze shipment
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-viewer");
        var response = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "Apples",
            productDescription = "",
            origin = "X",
            destination = "Y"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateShipment_NotMember_Returns403()
    {
        // Arrange — user-stranger NU e membru al organizatiei
        await CreateTestUserAsync("user-owner");
        await CreateTestUserAsync("user-stranger");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");

        // Act
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-stranger");
        var response = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "Apples",
            productDescription = "",
            origin = "X",
            destination = "Y"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShipment_MemberCanAccess_ReturnsOk()
    {
        // Arrange
        await CreateTestUserAsync("user-owner");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");

        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "Test Product",
            productDescription = "",
            origin = "A",
            destination = "B"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Shipment>();

        // Act
        var response = await _client.GetAsync($"/api/shipments/{created!.ShipmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrieved = await response.Content.ReadFromJsonAsync<Shipment>();
        retrieved!.ShipmentId.Should().Be(created.ShipmentId);
        retrieved.ProductName.Should().Be("Test Product");
    }

    [Fact]
    public async Task GetShipment_NotMember_Returns403()
    {
        // Arrange — shipment creat de user-A, accesat de stranger
        await CreateTestUserAsync("user-owner");
        await CreateTestUserAsync("user-stranger");
        var orgId = await CreateOrgAsync("user-owner", "TestOrg");

        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-owner");
        var createResponse = await _client.PostAsJsonAsync("/api/shipments", new
        {
            organizationId = orgId,
            productName = "P", productDescription = "", origin = "X", destination = "Y"
        });
        var created = await createResponse.Content.ReadFromJsonAsync<Shipment>();

        // Act
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-stranger");
        var response = await _client.GetAsync($"/api/shipments/{created!.ShipmentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetShipment_NonExistentId_Returns403OrNotFound()
    {
        // Arrange — userul exista dar shipment-ul nu
        await CreateTestUserAsync("user-1");
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-1");

        // Act
        var response = await _client.GetAsync("/api/shipments/non-existent-id");

        // Assert — authorization check returneaza 403 pentru ca shipment-ul nu exista
        // (in CanAccessShipmentAsync, daca shipment == null returneaza false → 403)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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

    private async Task AddMemberAsync(string orgId, string userId, string role)
    {
        // Adauga direct in Firestore pentru simplicitate (evita Owner-only check)
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

        // Adauga si in user
        var userRef = _fixture.Firestore.Collection("Users").Document(userId);
        var userSnap = await userRef.GetSnapshotAsync();
        var user = userSnap.ConvertTo<User>();
        if (!user.OrganizationIds.Contains(orgId))
        {
            user.OrganizationIds.Add(orgId);
            await userRef.SetAsync(user);
        }
    }
}