using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Sensor
{
    [FirestoreProperty]
    public string Ieee { get; set; } = string.Empty;          // ID din Zigbee
    
    [FirestoreProperty]
    public string LogicalId { get; set; } = string.Empty;     // ex: "temp_rack_1"
    
    [FirestoreProperty]
    public string DisplayName { get; set; } = string.Empty;   // ex: "Temperatură frigider 1"
    
    [FirestoreProperty]
    public string SensorType { get; set; } = string.Empty;    // SensorType enum
    
    [FirestoreProperty]
    public string Unit { get; set; } = string.Empty;          // ex: "°C"
    
    [FirestoreProperty]
    public string? AssignedShipmentId { get; set; }            // null dacă nu e atribuit
    
    [FirestoreProperty]
    public string Status { get; set; } = "Pending";            // "Pending", "Active", "Inactive"
    
    [FirestoreProperty]
    public Timestamp DiscoveredAt { get; set; }
    
    [FirestoreProperty]
    public Timestamp? PairedAt { get; set; }
}