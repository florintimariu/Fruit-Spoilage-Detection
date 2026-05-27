// using Backend.Models;
// using Backend.Services;
// using Backend.Services.Interfaces;
// using FluentAssertions;
// using Moq;

// namespace Backend.Tests.UnitTests;

// public class ShipmentServiceTests
// {
//     private readonly Mock<IShipmentRepository> _shipmentRepoMock;
//     private readonly ShipmentService _shipmentService;

//     public ShipmentServiceTests()
//     {
//         // Arrange (constructor): creem mock-urile o singura data pentru toate testele
//         _shipmentRepoMock = new Mock<IShipmentRepository>();
//         _shipmentService = new ShipmentService(_shipmentRepoMock.Object);
//     }

//     [Fact]
//     public async Task GetShipmentAsync_ExistingShipment_ReturnsShipment()
//     {
//         // Arrange — pregatim datele de test
//         var shipmentId = "shipment-123";
//         var expectedShipment = new Shipment
//         {
//             ShipmentId = shipmentId,
//             ProductName = "Apples",
//             OrganizationId = "org-1",
//             Status = "Created"
//         };

//         _shipmentRepoMock
//             .Setup(repo => repo.GetByIdAsync(shipmentId))
//             .ReturnsAsync(expectedShipment);

//         // Act — executam metoda testata
//         var result = await _shipmentService.GetShipmentAsync(shipmentId);

//         // Assert — verificam rezultatul
//         result.Should().NotBeNull();
//         result!.ShipmentId.Should().Be(shipmentId);
//         result.ProductName.Should().Be("Apples");
//         result.Status.Should().Be("Created");

//         // Verificam ca repository-ul a fost apelat exact o data
//         _shipmentRepoMock.Verify(repo => repo.GetByIdAsync(shipmentId), Times.Once);
//     }

//     [Fact]
//     public async Task GetShipmentAsync_NonExistingShipment_ReturnsNull()
//     {
//         // Arrange
//         var shipmentId = "nonexistent";
//         _shipmentRepoMock
//             .Setup(repo => repo.GetByIdAsync(shipmentId))
//             .ReturnsAsync((Shipment?)null);

//         // Act
//         var result = await _shipmentService.GetShipmentAsync(shipmentId);

//         // Assert
//         result.Should().BeNull();
//     }
// }