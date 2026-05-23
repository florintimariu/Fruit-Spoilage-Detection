using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Services.Implementations;

public class SensorService : ISensorService
{
    private const string CollectionName = "Sensors";
    private readonly FirestoreDb _db;
    private readonly ILogger<SensorService> _logger;

    public SensorService(FirestoreDb db, ILogger<SensorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Sensor?> GetSensorAsync(string ieee)
    {
        var snapshot = await _db.Collection(CollectionName).Document(ieee).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Sensor>() : null;
    }

    public async Task<List<Sensor>> GetAllSensorsAsync()
    {
        var snapshot = await _db.Collection(CollectionName).GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Sensor>()).ToList();
    }

    public async Task<Sensor> RegisterSensorAsync(
        string ieee,
        string logicalId,
        string displayName,
        SensorType sensorType,
        string unit)
    {
        var sensor = new Sensor
        {
            Ieee = ieee,
            LogicalId = logicalId,
            DisplayName = displayName,
            SensorType = sensorType.ToString(),
            Unit = unit,
            Status = "Active",
            DiscoveredAt = Timestamp.GetCurrentTimestamp(),
            PairedAt = Timestamp.GetCurrentTimestamp()
        };

        await _db.Collection(CollectionName).Document(ieee).SetAsync(sensor);
        _logger.LogInformation("Registered sensor {Ieee} ({DisplayName})", ieee, displayName);
        return sensor;
    }

    public async Task<bool> AssignToShipmentAsync(string ieee, string shipmentId)
    {
        try
        {
            await _db.Collection(CollectionName).Document(ieee).UpdateAsync(
                new Dictionary<string, object>
                {
                    { "AssignedShipmentId", shipmentId }
                });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign sensor {Ieee} to {ShipmentId}", ieee, shipmentId);
            return false;
        }
    }

    public async Task<bool> UnassignFromShipmentAsync(string ieee)
    {
        try
        {
            await _db.Collection(CollectionName).Document(ieee).UpdateAsync(
                new Dictionary<string, object>
                {
                    { "AssignedShipmentId", FieldValue.Delete }
                });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unassign sensor {Ieee}", ieee);
            return false;
        }
    }
}