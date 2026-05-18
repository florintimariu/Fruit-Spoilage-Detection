using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Shipment
{
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ProductName { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ProductDescription { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Origin { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Destination { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Status { get; set; } = "Created"; // ShipmentStatus enum ca string
    
    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
    
    [FirestoreProperty]
    public Timestamp? CompletedAt { get; set; }
    
    [FirestoreProperty]
    public string? CurrentStepId { get; set; } // step-ul activ în acest moment
    
    [FirestoreProperty]
    public string OwnerId { get; set; } = string.Empty; // user care a creat shipment-ul
}