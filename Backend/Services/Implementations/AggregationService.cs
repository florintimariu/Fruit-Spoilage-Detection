using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class AggregationService : IAggregationService
{
    private const string ShipmentsCollection = "Shipments";
    private readonly FirestoreDb _db;
    private readonly ILogger<AggregationService> _logger;

    public AggregationService(FirestoreDb db, ILogger<AggregationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OverviewStats> GetOverviewAsync(
        string organizationId,
        DateTime from,
        DateTime to)
    {
        var fromTimestamp = Timestamp.FromDateTime(from.ToUniversalTime());
        var toTimestamp = Timestamp.FromDateTime(to.ToUniversalTime());

        var query = _db.Collection(ShipmentsCollection)
            .WhereEqualTo("OrganizationId", organizationId)
            .WhereGreaterThanOrEqualTo("CreatedAt", fromTimestamp)
            .WhereLessThanOrEqualTo("CreatedAt", toTimestamp);

        var snapshot = await query.GetSnapshotAsync();
        var shipments = snapshot.Documents.Select(d => d.ConvertTo<Shipment>()).ToList();

        return new OverviewStats(
            TotalShipments: shipments.Count,
            CompletedShipments: shipments.Count(s => s.Status == "Completed"),
            InProgressShipments: shipments.Count(s => s.Status == "InProgress"),
            CompromisedShipments: shipments.Count(s => s.Status == "Compromised"),
            CreatedShipments: shipments.Count(s => s.Status == "Created"),
            PeriodFrom: from,
            PeriodTo: to);
    }
}