using Backend.Services.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Backend.Tests.Integration;

[Collection("Firestore")]
public class ShipmentServiceIntegrationTests : IAsyncLifetime
{
    private readonly FirestoreEmulatorFixture _fixture;
    private readonly ShipmentService _sut;

    public ShipmentServiceIntegrationTests(FirestoreEmulatorFixture fixture)
    {
        _fixture = fixture;
        _sut = new ShipmentService(_fixture.Firestore, NullLogger<ShipmentService>.Instance);
    }

    // Curatam datele inainte de fiecare test pentru izolare
    public Task InitializeAsync() => _fixture.ClearAllCollectionsAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateShipment_PersistsToFirestore_AndCanBeRetrieved()
    {
        // Arrange & Act
        var created = await _sut.CreateShipmentAsync(
            organizationId: "org-1",
            productName: "Apples Golden",
            productDescription: "10 crates",
            origin: "Cluj",
            destination: "Bucuresti",
            createdByUserId: "user-1");

        // Assert — proprietatile basic
        created.Should().NotBeNull();
        created.ShipmentId.Should().NotBeNullOrWhiteSpace();
        created.OrganizationId.Should().Be("org-1");
        created.ProductName.Should().Be("Apples Golden");
        created.Status.Should().Be("Created");

        // Assert — persistat in Firestore
        var retrieved = await _sut.GetShipmentAsync(created.ShipmentId);
        retrieved.Should().NotBeNull();
        retrieved!.ShipmentId.Should().Be(created.ShipmentId);
        retrieved.ProductName.Should().Be("Apples Golden");
        retrieved.Origin.Should().Be("Cluj");
        retrieved.Destination.Should().Be("Bucuresti");
        retrieved.CreatedByUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task GetShipment_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetShipmentAsync("does-not-exist");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetShipmentsForOrganization_ReturnsOnlyMatchingShipments()
    {
        // Arrange — creem 3 shipments in 2 organizatii diferite
        await _sut.CreateShipmentAsync("org-A", "Apples", "", "X", "Y", "u1");
        await _sut.CreateShipmentAsync("org-A", "Bananas", "", "X", "Y", "u1");
        await _sut.CreateShipmentAsync("org-B", "Oranges", "", "X", "Y", "u2");

        // Act
        var shipmentsA = await _sut.GetShipmentsForOrganizationAsync("org-A");
        var shipmentsB = await _sut.GetShipmentsForOrganizationAsync("org-B");

        // Assert
        shipmentsA.Should().HaveCount(2);
        shipmentsA.Select(s => s.ProductName).Should().BeEquivalentTo(new[] { "Apples", "Bananas" });
        
        shipmentsB.Should().HaveCount(1);
        shipmentsB[0].ProductName.Should().Be("Oranges");
    }

    [Fact]
    public async Task GetShipmentsForOrganization_NoShipments_ReturnsEmpty()
    {
        // Act
        var result = await _sut.GetShipmentsForOrganizationAsync("org-empty");

        // Assert
        result.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task UpdateStatus_ChangesStatusInFirestore()
    {
        // Arrange
        var shipment = await _sut.CreateShipmentAsync("org-1", "P", "", "X", "Y", "u");
        
        // Act
        var success = await _sut.UpdateStatusAsync(shipment.ShipmentId, "InProgress");
        var retrieved = await _sut.GetShipmentAsync(shipment.ShipmentId);

        // Assert
        success.Should().BeTrue();
        retrieved!.Status.Should().Be("InProgress");
    }

    [Fact]
    public async Task UpdateStatus_ToCompleted_SetsCompletedAt()
    {
        // Arrange
        var shipment = await _sut.CreateShipmentAsync("org-1", "P", "", "X", "Y", "u");

        // Act
        await _sut.UpdateStatusAsync(shipment.ShipmentId, "Completed");
        var retrieved = await _sut.GetShipmentAsync(shipment.ShipmentId);

        // Assert
        retrieved!.Status.Should().Be("Completed");
        retrieved.CompletedAt.Should().NotBeNull();
    }
}