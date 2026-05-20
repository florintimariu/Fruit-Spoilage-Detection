using Backend.Models;
using Backend.Common.Enums;

namespace Backend.Services.Interfaces;

public interface ISensorService
{
    Task<Sensor?> GetSensorAsync(string ieee);
    Task<List<Sensor>> GetAllSensorsAsync();
    Task<Sensor> RegisterSensorAsync(
        string ieee,
        string logicalId,
        string displayName,
        SensorType sensorType,
        string unit);
    Task<bool> AssignToShipmentAsync(string ieee, string shipmentId);
    Task<bool> UnassignFromShipmentAsync(string ieee);
}