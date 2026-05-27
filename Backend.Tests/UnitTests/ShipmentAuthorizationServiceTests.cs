using Backend.Common.Enums;
using Backend.Models;
using Backend.Services.Implementations;
using Backend.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace Backend.Tests.UnitTests;

public class ShipmentAuthorizationServiceTests
{
    private readonly Mock<IShipmentService> _shipmentServiceMock;
    private readonly Mock<IOrganizationService> _orgServiceMock;
    private readonly ShipmentAuthorizationService _sut;

    public ShipmentAuthorizationServiceTests()
    {
        _shipmentServiceMock = new Mock<IShipmentService>();
        _orgServiceMock = new Mock<IOrganizationService>();
        _sut = new ShipmentAuthorizationService(_shipmentServiceMock.Object, _orgServiceMock.Object);
    }

    // ===== CanAccessShipmentAsync =====

    [Fact]
    public async Task CanAccess_ShipmentNotFound_ReturnsFalse()
    {
        // Arrange
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync("non-existent"))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _sut.CanAccessShipmentAsync("user-1", "non-existent");

        // Assert
        result.Should().BeFalse();
        // Nu trebuie sa ajunga la a verifica organization membership
        _orgServiceMock.Verify(o => o.IsUserMemberAsync(It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never);
    }

    [Fact]
    public async Task CanAccess_UserIsMember_ReturnsTrue()
    {
        // Arrange
        var shipment = new Shipment { ShipmentId = "ship-1", OrganizationId = "org-1" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync("ship-1"))
            .ReturnsAsync(shipment);
        _orgServiceMock
            .Setup(o => o.IsUserMemberAsync("org-1", "user-1"))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.CanAccessShipmentAsync("user-1", "ship-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccess_UserNotMember_ReturnsFalse()
    {
        // Arrange
        var shipment = new Shipment { ShipmentId = "ship-1", OrganizationId = "org-1" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync("ship-1"))
            .ReturnsAsync(shipment);
        _orgServiceMock
            .Setup(o => o.IsUserMemberAsync("org-1", "user-stranger"))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.CanAccessShipmentAsync("user-stranger", "ship-1");

        // Assert
        result.Should().BeFalse();
    }

    // ===== CanModifyShipmentAsync =====

    [Fact]
    public async Task CanModify_ShipmentNotFound_ReturnsFalse()
    {
        // Arrange
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync("ship-1"))
            .ReturnsAsync((Shipment?)null);

        // Act
        var result = await _sut.CanModifyShipmentAsync("user-1", "ship-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanModify_UserIsOwner_ReturnsTrue()
    {
        // Arrange — Owner poate modifica
        SetupShipmentWithRole("ship-1", "org-1", "user-owner", OrganizationRole.Owner);

        // Act
        var result = await _sut.CanModifyShipmentAsync("user-owner", "ship-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModify_UserIsOperator_ReturnsTrue()
    {
        // Arrange — Operator poate modifica
        SetupShipmentWithRole("ship-1", "org-1", "user-operator", OrganizationRole.Operator);

        // Act
        var result = await _sut.CanModifyShipmentAsync("user-operator", "ship-1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanModify_UserIsViewer_ReturnsFalse()
    {
        // Arrange — Viewer NU poate modifica
        SetupShipmentWithRole("ship-1", "org-1", "user-viewer", OrganizationRole.Viewer);

        // Act
        var result = await _sut.CanModifyShipmentAsync("user-viewer", "ship-1");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanModify_UserNotMember_ReturnsFalse()
    {
        // Arrange — user fara nicio rol in organizatie
        var shipment = new Shipment { ShipmentId = "ship-1", OrganizationId = "org-1" };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync("ship-1"))
            .ReturnsAsync(shipment);
        _orgServiceMock
            .Setup(o => o.GetUserRoleAsync("org-1", "stranger"))
            .ReturnsAsync((OrganizationRole?)null);

        // Act
        var result = await _sut.CanModifyShipmentAsync("stranger", "ship-1");

        // Assert
        result.Should().BeFalse();
    }

    // Helper privat — reduce duplicarea pentru testele Modify
    private void SetupShipmentWithRole(string shipmentId, string orgId, string userId, OrganizationRole role)
    {
        var shipment = new Shipment { ShipmentId = shipmentId, OrganizationId = orgId };
        _shipmentServiceMock
            .Setup(s => s.GetShipmentAsync(shipmentId))
            .ReturnsAsync(shipment);
        _orgServiceMock
            .Setup(o => o.GetUserRoleAsync(orgId, userId))
            .ReturnsAsync(role);
    }
}