using System.Net;
using System.Net.Http.Json;
using Backend.Models;
using FluentAssertions;
using Google.Cloud.Firestore;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class ReadingEndpointsTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReadingEndpointsTests(FirestoreEmulatorFixture fixture)
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
    public async Task BatchUpload_ValidReadings_ReturnsOk()
    {
        // Arrange
        await CreateTestUserAsync("rpi-device");
        var orgId = await CreateOrgAsync("rpi-device", "TestOrg");
        var shipmentId = await CreateShipmentAsync("rpi-device", orgId);
        var stepId = await StartStepAsync("rpi-device", shipmentId);

        // Act — RPI trimite un batch de citiri
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "rpi-device");
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/readings/batch",
            new
            {
                readings = new[]
                {
                    new { sensorIeee = "0x00124b001234", sensorLogicalId = "temp_1",
                          sensorType = "Temperature", value = 4.5, unit = "°C" },
                    new { sensorIeee = "0x00124b005678", sensorLogicalId = "hum_1",
                          sensorType = "Humidity", value = 72.3, unit = "%" },
                    new { sensorIeee = "0x00124b009abc", sensorLogicalId = "mq3_1",
                          sensorType = "VocEthanol", value = 0.12, unit = "ppm" }
                },
                location = new { latitude = 46.7712, longitude = 23.6236,
                                 accuracy = 5.0, speed = 0.0 }
            });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BatchUpload_CompletedStep_Returns400()
    {
        // Arrange — completam step-ul inainte de a trimite citiri
        await CreateTestUserAsync("rpi-device");
        var orgId = await CreateOrgAsync("rpi-device", "TestOrg");
        var shipmentId = await CreateShipmentAsync("rpi-device", orgId);
        var stepId = await StartStepAsync("rpi-device", shipmentId);

        // Completam step-ul
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "rpi-device");
        await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/complete",
            new { aiStatus = "Fresh" });

        // Act — incercam sa trimitem citiri la un step deja completat
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/readings/batch",
            new
            {
                readings = new[]
                {
                    new { sensorIeee = "0x001", sensorLogicalId = "temp_1",
                          sensorType = "Temperature", value = 5.0, unit = "°C" }
                },
                location = (object?)null
            });

        // Assert — nu se pot adauga citiri la un step completat
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BatchUpload_Unauthorized_Returns401()
    {
        // Act — fara header de autentificare
        var response = await _client.PostAsJsonAsync(
            "/api/shipments/any/steps/any/readings/batch",
            new { readings = Array.Empty<object>(), location = (object?)null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReadings_AfterBatchUpload_ReturnsPersistedReadings()
    {
        // Arrange
        await CreateTestUserAsync("rpi-device");
        var orgId = await CreateOrgAsync("rpi-device", "TestOrg");
        var shipmentId = await CreateShipmentAsync("rpi-device", orgId);
        var stepId = await StartStepAsync("rpi-device", shipmentId);

        // Inregistram senzorii in Firestore INAINTE de batch upload
        await RegisterSensorAsync("0x00124b001234", "temp_1", "Temperature", "°C");
        await RegisterSensorAsync("0x00124b005678", "hum_1", "Humidity", "%");

        // Upload batch
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "rpi-device");
        var uploadResponse = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/readings/batch",
            new
            {
                readings = new[]
                {
                    new { sensorIeee = "0x00124b001234", sensorLogicalId = "temp_1",
                        sensorType = "Temperature", value = 4.5, unit = "°C",
                        timestamp = DateTime.UtcNow },
                    new { sensorIeee = "0x00124b005678", sensorLogicalId = "hum_1",
                        sensorType = "Humidity", value = 72.3, unit = "%",
                        timestamp = DateTime.UtcNow }
                },
                location = (object?)null
            });

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await _client.GetAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/readings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var readings = await response.Content.ReadFromJsonAsync<List<SensorReading>>();
        readings.Should().NotBeNull();
        readings!.Should().HaveCount(2);
        readings.Select(r => r.SensorType)
            .Should().BeEquivalentTo(new[] { "Temperature", "Humidity" });
    }

    [Fact]
    public async Task GetLocations_AfterBatchUpload_ReturnsPersistedLocation()
    {
        // Arrange
        await CreateTestUserAsync("rpi-device");
        var orgId = await CreateOrgAsync("rpi-device", "TestOrg");
        var shipmentId = await CreateShipmentAsync("rpi-device", orgId);
        var stepId = await StartStepAsync("rpi-device", shipmentId);

        // Upload batch cu locatie GPS
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", "rpi-device");
        await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/readings/batch",
            new
            {
                readings = Array.Empty<object>(),
                location = new
                {
                    latitude = 46.7712,
                    longitude = 23.6236,
                    accuracy = 5.0,
                    speed = 12.5
                }
            });

        // Act
        var response = await _client.GetAsync(
            $"/api/shipments/{shipmentId}/steps/{stepId}/locations");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var locations = await response.Content.ReadFromJsonAsync<List<Location>>();
        locations.Should().NotBeNull();
        locations!.Should().HaveCount(1);
        locations[0].Latitude.Should().BeApproximately(46.7712, 0.0001);
        locations[0].Longitude.Should().BeApproximately(23.6236, 0.0001);
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
            origin = "Cluj",
            destination = "Bucuresti"
        });
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        return shipment!.ShipmentId;
    }

    private async Task<string> StartStepAsync(string userId, string shipmentId)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        _client.DefaultRequestHeaders.Add("X-Test-User-Id", userId);
        var response = await _client.PostAsJsonAsync(
            $"/api/shipments/{shipmentId}/steps",
            new { type = "Transport", locationName = "Cluj", operatorName = "RPI" });
        var step = await response.Content.ReadFromJsonAsync<Step>();
        _client.DefaultRequestHeaders.Remove("X-Test-User-Id");
        return step!.StepId;
    }

     private async Task RegisterSensorAsync(              // ← adaugat aici
        string ieee, string logicalId, string sensorType, string unit)
    {
        var sensor = new Sensor
        {
            Ieee = ieee,
            LogicalId = logicalId,
            DisplayName = logicalId,
            SensorType = sensorType,
            Unit = unit,
            Status = "Active",
            DiscoveredAt = Timestamp.GetCurrentTimestamp()
        };
        await _fixture.Firestore.Collection("Sensors").Document(ieee).SetAsync(sensor);
    }
}