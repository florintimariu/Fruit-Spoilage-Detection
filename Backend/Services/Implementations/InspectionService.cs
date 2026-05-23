using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Services.Implementations;

public class InspectionService : IInspectionService
{
    private const string ShipmentsCollection = "Shipments";
    private const string InspectionsSubcollection = "AiInspections";

    private readonly FirestoreDb _db;
    private readonly IShipmentService _shipmentService;
    private readonly ILogger<InspectionService> _logger;

    private readonly INotificationService _notificationService;

    public InspectionService(
        FirestoreDb db,
        IShipmentService shipmentService,
        INotificationService notificationService,
        ILogger<InspectionService> logger)
    {
        _db = db;
        _shipmentService = shipmentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<AiInspection> CreateInspectionAsync(
        string shipmentId,
        string? stepId,
        string imageUrl,
        string maskUrl,
        AiVerdict verdict,
        double spoilagePercent,
        string triggerType)
    {
        var inspectionId = Guid.NewGuid().ToString();
        var inspection = new AiInspection
        {
            InspectionId = inspectionId,
            ShipmentId = shipmentId,
            StepId = stepId,
            ImageUrl = imageUrl,
            MaskUrl = maskUrl,
            Verdict = verdict.ToString(),
            SpoilagePercent = spoilagePercent,
            SpoilageDetected = verdict != AiVerdict.Fresh,
            Timestamp = Timestamp.GetCurrentTimestamp(),
            TriggerType = triggerType
        };

        await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(InspectionsSubcollection)
            .Document(inspectionId)
            .SetAsync(inspection);

        // Daca e spoiled, marcheaza shipment-ul ca Compromised
        if (verdict == AiVerdict.Spoiled)
        {
            await _shipmentService.UpdateStatusAsync(shipmentId, "Compromised");
            _logger.LogWarning(
                "Shipment {ShipmentId} marked COMPROMISED due to spoilage detection ({Percent}%)",
                shipmentId, spoilagePercent);

            // Trimite notificare push catre organizatie
            var shipment = await _shipmentService.GetShipmentAsync(shipmentId);
            if (shipment != null)
            {
                await _notificationService.SendToOrganizationAsync(
                    shipment.OrganizationId,
                    "Alert: Product is compromised",
                    $"Product '{shipment.ProductName}' has been detected as spoiled ({spoilagePercent:F0}% deterioration)",
                    new Dictionary<string, string>
                    {
                        { "shipmentId", shipmentId },
                        { "type", "shipment_compromised" }
                    });
            }
        }

        _logger.LogInformation(
            "Created inspection {InspectionId} for shipment {ShipmentId}: {Verdict} ({Percent}%)",
            inspectionId, shipmentId, verdict, spoilagePercent);

        return inspection;
    }

    public async Task<List<AiInspection>> GetInspectionsForShipmentAsync(string shipmentId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(InspectionsSubcollection)
            .OrderByDescending("Timestamp")
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<AiInspection>()).ToList();
    }

    public async Task<AiInspection?> GetInspectionAsync(string shipmentId, string inspectionId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(InspectionsSubcollection)
            .Document(inspectionId)
            .GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<AiInspection>() : null;
    }
}