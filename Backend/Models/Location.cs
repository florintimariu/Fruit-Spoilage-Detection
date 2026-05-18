using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Location
{
    [FirestoreProperty]
    public string LocationId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string StepId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public double Latitude { get; set; }
    
    [FirestoreProperty]
    public double Longitude { get; set; }
    
    [FirestoreProperty]
    public double? Accuracy { get; set; }  // în metri
    
    [FirestoreProperty]
    public double? Speed { get; set; }     // în m/s
    
    [FirestoreProperty]
    public Timestamp Timestamp { get; set; }
}