using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class ReadingService : IReadingService
{
    private const string ShipmentsCollection = "Shipments";
    private const string StepsSubcollection = "Steps";
    private const string ReadingsSubcollection = "SensorReadings";
    private const string LocationsSubcollection = "Locations";

    private readonly FirestoreDb _db;
    private readonly ISensorService _sensorService;
    private readonly ILogger<ReadingService> _logger;

    public ReadingService(
        FirestoreDb db,
        ISensorService sensorService,
        ILogger<ReadingService> logger)
    {
        _db = db;
        _sensorService = sensorService;
        _logger = logger;
    }

    public async Task<BatchResult> ProcessBatchAsync(
        string shipmentId,
        string stepId,
        List<ReadingInput> readings,
        LocationInput? location)
    {
        var warnings = new List<string>();
        var accepted = 0;
        var rejected = 0;
        var stepDocRef = _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId);

        // Batch write pentru performanta
        var batch = _db.StartBatch();

        // Procesare readings
        foreach (var reading in readings)
        {
            var sensor = await _sensorService.GetSensorAsync(reading.SensorIeee);
            if (sensor == null)
            {
                warnings.Add($"Sensor {reading.SensorIeee} not registered, skipping");
                rejected++;
                continue;
            }

            var readingId = Guid.NewGuid().ToString();
            var sensorReading = new SensorReading
            {
                ReadingId = readingId,
                ShipmentId = shipmentId,
                StepId = stepId,
                SensorIeee = reading.SensorIeee,
                SensorLogicalId = sensor.LogicalId,
                SensorType = sensor.SensorType,
                Value = reading.Value,
                Unit = sensor.Unit,
                Timestamp = Timestamp.FromDateTime(reading.Timestamp.ToUniversalTime())
            };

            var readingDocRef = stepDocRef
                .Collection(ReadingsSubcollection)
                .Document(readingId);
            batch.Set(readingDocRef, sensorReading);
            accepted++;
        }

        // Procesare locatie GPS (daca exista)
        var locationAccepted = false;
        if (location != null)
        {
            var locationId = Guid.NewGuid().ToString();
            var locationDoc = new Location
            {
                LocationId = locationId,
                ShipmentId = shipmentId,
                StepId = stepId,
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                Accuracy = location.Accuracy,
                Speed = location.Speed,
                Timestamp = Timestamp.FromDateTime(location.Timestamp.ToUniversalTime())
            };

            var locationDocRef = stepDocRef
                .Collection(LocationsSubcollection)
                .Document(locationId);
            batch.Set(locationDocRef, locationDoc);
            locationAccepted = true;
        }

        // Commit batch
        await batch.CommitAsync();

        _logger.LogInformation(
            "Processed batch for step {StepId}: {Accepted} readings accepted, {Rejected} rejected, location: {Location}",
            stepId, accepted, rejected, locationAccepted);

        return new BatchResult(accepted, rejected, locationAccepted, warnings);
    }

    public async Task<List<SensorReading>> GetReadingsForStepAsync(string shipmentId, string stepId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .Collection(ReadingsSubcollection)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<SensorReading>()).ToList();
    }

    public async Task<List<Location>> GetLocationsForStepAsync(string shipmentId, string stepId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .Collection(LocationsSubcollection)
            .OrderBy("Timestamp")
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Location>()).ToList();
    }
}