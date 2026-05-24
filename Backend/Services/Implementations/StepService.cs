using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Services.Implementations;

public class StepService : IStepService
{
    private const string ShipmentsCollection = "Shipments";
    private const string StepsSubcollection = "Steps";
    private const string ReadingsSubcollection = "SensorReadings";
    
    private readonly FirestoreDb _db;
    private readonly IShipmentService _shipmentService;
    private readonly IBlockchainService _blockchainService;
    private readonly IHashingService _hashingService;
    private readonly ILogger<StepService> _logger;

    public StepService(
        FirestoreDb db,
        IShipmentService shipmentService,
        IBlockchainService blockchainService,
        IHashingService hashingService,
        ILogger<StepService> logger)
    {
        _db = db;
        _shipmentService = shipmentService;
        _blockchainService = blockchainService;
        _hashingService = hashingService;
        _logger = logger;
    }

    public async Task<Step?> GetStepAsync(string shipmentId, string stepId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<Step>() : null;
    }

    public async Task<List<Step>> GetStepsForShipmentAsync(string shipmentId)
    {
        var snapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<Step>()).ToList();
    }

    public async Task<Step> StartStepAsync(
        string shipmentId,
        StepType type,
        string locationName,
        string operatorName)
    {
        var stepId = Guid.NewGuid().ToString();
        var step = new Step
        {
            StepId = stepId,
            ShipmentId = shipmentId,
            Type = type.ToString(),
            LocationName = locationName,
            OperatorName = operatorName,
            StartTime = Timestamp.GetCurrentTimestamp(),
            IsCompleted = false,
            ReadingsCount = 0
        };

        await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .SetAsync(step);

        // Update shipment current step
        await _shipmentService.SetCurrentStepAsync(shipmentId, stepId);
        await _shipmentService.UpdateStatusAsync(shipmentId, "InProgress");

        _logger.LogInformation(
            "Started step {StepId} ({Type}) for shipment {ShipmentId}",
            stepId, type, shipmentId);
        return step;
    }

    public async Task<StepCompletionResult> CompleteStepAsync(
        string shipmentId,
        string stepId,
        string aiStatus)
    {
        var step = await GetStepAsync(shipmentId, stepId);
        if (step == null)
        {
            return new StepCompletionResult(
                null!, null, false, "Step not found");
        }

        if (step.IsCompleted)
        {
            return new StepCompletionResult(
                step, null, false, "Step already completed");
        }

        // 1. Calculate aggregates from readings
        var readingsSnapshot = await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .Collection(ReadingsSubcollection)
            .GetSnapshotAsync();

        var readings = readingsSnapshot.Documents
            .Select(d => d.ConvertTo<SensorReading>())
            .ToList();

        var tempReadings = readings.Where(r => r.SensorType == "Temperature").ToList();
        var humidityReadings = readings.Where(r => r.SensorType == "Humidity").ToList();

        step.MinTemp = tempReadings.Any() ? tempReadings.Min(r => r.Value) : (double?)null;
        step.MaxTemp = tempReadings.Any() ? tempReadings.Max(r => r.Value) : (double?)null;
        step.AvgTemp = tempReadings.Any() ? tempReadings.Average(r => r.Value) : (double?)null;
        step.MinHumidity = humidityReadings.Any() ? humidityReadings.Min(r => r.Value) : (double?)null;
        step.MaxHumidity = humidityReadings.Any() ? humidityReadings.Max(r => r.Value) : (double?)null;
        step.AvgHumidity = humidityReadings.Any() ? humidityReadings.Average(r => r.Value) : (double?)null;
        step.ReadingsCount = readings.Count;
        step.EndTime = Timestamp.GetCurrentTimestamp();
        step.IsCompleted = true;
        step.AiStatusAtCompletion = aiStatus;

        // 2. Compute hash of step data
        var dataHash = _hashingService.ComputeKeccak256(new
        {
            ShipmentId = step.ShipmentId,
            StepId = step.StepId,
            Type = step.Type,
            LocationName = step.LocationName,
            OperatorName = step.OperatorName,
            MinTemp = step.MinTemp,
            MaxTemp = step.MaxTemp,
            AvgTemp = step.AvgTemp,
            MinHumidity = step.MinHumidity,
            MaxHumidity = step.MaxHumidity,
            AvgHumidity = step.AvgHumidity,
            ReadingsCount = step.ReadingsCount,
            AiStatus = aiStatus
        });
        step.DataHash = _hashingService.ToHexString(dataHash);

        // 3. Anchor on blockchain
        var anchorResult = await _blockchainService.AnchorStepAsync(
            shipmentId, stepId, aiStatus, dataHash);

        if (anchorResult.Success)
        {
            step.TransactionHash = anchorResult.TransactionHash;
            step.AnchoredAt = Timestamp.GetCurrentTimestamp();
        }

        // 4. Save step
        await _db.Collection(ShipmentsCollection)
            .Document(shipmentId)
            .Collection(StepsSubcollection)
            .Document(stepId)
            .SetAsync(step);

        _logger.LogInformation(
            "Completed step {StepId} for shipment {ShipmentId}. Anchored: {Anchored}",
            stepId, shipmentId, anchorResult.Success);

        return new StepCompletionResult(
            step,
            anchorResult.Success ? anchorResult.TransactionHash : null,
            anchorResult.Success,
            anchorResult.ErrorMessage);
    }
}