using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Shipment
{
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string OrganizationId { get; set; } = string.Empty;  // schimbat din OwnerId
    
    [FirestoreProperty]
    public string ProductName { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ProductDescription { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Origin { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Destination { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Status { get; set; } = "Created";
    
    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
    
    [FirestoreProperty]
    public Timestamp? CompletedAt { get; set; }
    
    [FirestoreProperty]
    public string? CurrentStepId { get; set; }
    
    [FirestoreProperty]
    public string CreatedByUserId { get; set; } = string.Empty;  // cine a creat shipment-ul
}