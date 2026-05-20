using Backend.Models;

namespace Backend.Services.Interfaces;

public interface IReadingService
{
    Task<BatchResult> ProcessBatchAsync(
        string shipmentId,
        string stepId,
        List<ReadingInput> readings,
        LocationInput? location);
    
    Task<List<SensorReading>> GetReadingsForStepAsync(string shipmentId, string stepId);
    Task<List<Location>> GetLocationsForStepAsync(string shipmentId, string stepId);
}

public record ReadingInput(
    string SensorIeee,
    double Value,
    DateTime Timestamp);

public record LocationInput(
    double Latitude,
    double Longitude,
    double? Accuracy,
    double? Speed,
    DateTime Timestamp);

public record BatchResult(
    int ReadingsAccepted,
    int ReadingsRejected,
    bool LocationAccepted,
    List<string> Warnings);