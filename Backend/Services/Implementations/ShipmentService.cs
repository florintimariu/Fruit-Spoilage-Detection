using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class ShipmentService : IShipmentService
{
    private const string CollectionName = "Shipments";
    private readonly FirestoreDb _db;
    private readonly ILogger<ShipmentService> _logger;

    public ShipmentService(FirestoreDb db, ILogger<ShipmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Shipment?> GetShipmentAsync(string shipmentId)
    {
        var snapshot = await _db.Collection(CollectionName).Document(shipmentId).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Shipment>() : null;
    }

    public async Task<List<Shipment>> GetShipmentsForOrganizationAsync(string organizationId)
    {
        var query = _db.Collection(CollectionName)
            .WhereEqualTo("OrganizationId", organizationId);
        var snapshot = await query.GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Shipment>()).ToList();
    }

    public async Task<Shipment> CreateShipmentAsync(
        string organizationId,
        string productName,
        string productDescription,
        string origin,
        string destination,
        string createdByUserId)
    {
        var shipmentId = Guid.NewGuid().ToString();
        var shipment = new Shipment
        {
            ShipmentId = shipmentId,
            OrganizationId = organizationId,
            ProductName = productName,
            ProductDescription = productDescription,
            Origin = origin,
            Destination = destination,
            Status = "Created",
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            CreatedByUserId = createdByUserId,
            CurrentStepId = null,
            CompletedAt = null
        };

        await _db.Collection(CollectionName).Document(shipmentId).SetAsync(shipment);
        _logger.LogInformation(
            "Created shipment {ShipmentId} for organization {OrgId}",
            shipmentId, organizationId);
        return shipment;
    }

    public async Task<bool> UpdateStatusAsync(string shipmentId, string status)
    {
        try
        {
            var updates = new Dictionary<string, object> { { "Status", status } };
            
            if (status == "Completed")
            {
                updates["CompletedAt"] = Timestamp.GetCurrentTimestamp();
            }
            
            await _db.Collection(CollectionName).Document(shipmentId).UpdateAsync(updates);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update shipment status for {ShipmentId}", shipmentId);
            return false;
        }
    }

    public async Task<bool> SetCurrentStepAsync(string shipmentId, string? stepId)
    {
        try
        {
            await _db.Collection(CollectionName).Document(shipmentId).UpdateAsync(
                new Dictionary<string, object> 
                { 
                    { "CurrentStepId", stepId! } 
                });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set current step for {ShipmentId}", shipmentId);
            return false;
        }
    }
}