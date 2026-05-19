namespace Backend.Services.Interfaces;

public interface IShipmentAuthorizationService
{
    Task<bool> CanAccessShipmentAsync(string userId, string shipmentId);
    Task<bool> CanModifyShipmentAsync(string userId, string shipmentId);
}