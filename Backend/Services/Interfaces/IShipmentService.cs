using Backend.Models;

namespace Backend.Services.Interfaces;

public interface IShipmentService
{
    Task<Shipment?> GetShipmentAsync(string shipmentId);
    Task<List<Shipment>> GetShipmentsForOrganizationAsync(string organizationId);
    Task<Shipment> CreateShipmentAsync(
        string organizationId,
        string productName,
        string productDescription,
        string origin,
        string destination,
        string createdByUserId);
    Task<bool> UpdateStatusAsync(string shipmentId, string status);
    Task<bool> SetCurrentStepAsync(string shipmentId, string? stepId);
}