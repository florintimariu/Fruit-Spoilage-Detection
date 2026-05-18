using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class SensorReading
{
    [FirestoreProperty]
    public string ReadingId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string StepId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string SensorIeee { get; set; } = string.Empty;     // hardware ID
    
    [FirestoreProperty]
    public string SensorLogicalId { get; set; } = string.Empty; // logical ID
    
    [FirestoreProperty]
    public string SensorType { get; set; } = string.Empty;      // SensorType enum
    
    [FirestoreProperty]
    public double Value { get; set; }
    
    [FirestoreProperty]
    public string Unit { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public Timestamp Timestamp { get; set; }
}