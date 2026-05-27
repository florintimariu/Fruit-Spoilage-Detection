using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class OrganizationEndpointsTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _firestoreFixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrganizationEndpointsTests(FirestoreEmulatorFixture firestoreFixture)
    {
        _firestoreFixture = firestoreFixture;
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public Task InitializeAsync() => _firestoreFixture.ClearAllCollectionsAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetOrganizations_Unauthorized_Returns401()
    {
        // Arrange — niciun header X-Test-User-Id

        // Act
        var response = await _client.GetAsync("/api/organizations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrganization_Authorized_ReturnsCreatedAndPersists()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-creator");
        var request = new
        {
            name = "Test Organization",
            description = "Created during testing"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<OrgResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Test Organization");
        body.CreatedByUserId.Should().Be("user-creator");
        body.OrganizationId.Should().NotBeNullOrWhiteSpace();

        // Verificam ca s-a persistat in Firestore
        var doc = await _firestoreFixture.Firestore
            .Collection("Organizations").Document(body.OrganizationId).GetSnapshotAsync();
        doc.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task CreateOrganization_MissingName_Returns400()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-1");
        var request = new { name = "", description = "no name" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
public async Task GetOrganizations_ReturnsOnlyUserOrganizations()
{
    // Arrange — creem user-ii in Firestore inainte (in mod normal e facut de middleware)
    await CreateTestUserAsync("user-A", "userA@test.com");
    await CreateTestUserAsync("user-B", "userB@test.com");

    // user-A creeaza 2 org
    _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-A");
    await _client.PostAsJsonAsync("/api/organizations", new { name = "Org A1" });
    await _client.PostAsJsonAsync("/api/organizations", new { name = "Org A2" });

    // user-B creeaza 1 org
    _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
    _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-B");
    await _client.PostAsJsonAsync("/api/organizations", new { name = "Org B1" });

    // Act — user-A vede doar org-urile sale
    _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
    _client.DefaultRequestHeaders.Add("X-Test-User-Id", "user-A");
    var response = await _client.GetAsync("/api/organizations");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var orgs = await response.Content.ReadFromJsonAsync<List<OrgResponse>>();
    orgs.Should().HaveCount(2);
    orgs!.Select(o => o.Name).Should().BeEquivalentTo(new[] { "Org A1", "Org A2" });
}

// Helper privat pentru a crea user-i in Firestore inainte de a-i folosi in teste
private async Task CreateTestUserAsync(string userId, string email)
{
    var user = new Backend.Models.User
    {
        UserId = userId,
        Email = email,
        DisplayName = userId,
        CreatedAt = Google.Cloud.Firestore.Timestamp.GetCurrentTimestamp(),
        OrganizationIds = new List<string>()
    };
    await _firestoreFixture.Firestore
        .Collection("Users").Document(userId).SetAsync(user);
}

    // DTO pentru deserializarea raspunsului
    private record OrgResponse(
        string OrganizationId,
        string Name,
        string Description,
        string CreatedByUserId);
}