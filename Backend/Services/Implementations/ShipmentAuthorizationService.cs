using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Services.Implementations;

public class ShipmentAuthorizationService : IShipmentAuthorizationService
{
    private readonly IShipmentService _shipmentService;
    private readonly IOrganizationService _orgService;

    public ShipmentAuthorizationService(
        IShipmentService shipmentService,
        IOrganizationService orgService)
    {
        _shipmentService = shipmentService;
        _orgService = orgService;
    }

    public async Task<bool> CanAccessShipmentAsync(string userId, string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentAsync(shipmentId);
        if (shipment == null) return false;
        return await _orgService.IsUserMemberAsync(shipment.OrganizationId, userId);
    }

    public async Task<bool> CanModifyShipmentAsync(string userId, string shipmentId)
    {
        var shipment = await _shipmentService.GetShipmentAsync(shipmentId);
        if (shipment == null) return false;
        
        var role = await _orgService.GetUserRoleAsync(shipment.OrganizationId, userId);
        return role == OrganizationRole.Owner || role == OrganizationRole.Operator;
    }
}