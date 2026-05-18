using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class AiInspection
{
    [FirestoreProperty]
    public string InspectionId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string? StepId { get; set; } // step în care s-a făcut inspecția
    
    [FirestoreProperty]
    public string ImageUrl { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string MaskUrl { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Verdict { get; set; } = string.Empty; // AiVerdict enum
    
    [FirestoreProperty]
    public double SpoilagePercent { get; set; } // 0.0 - 100.0
    
    [FirestoreProperty]
    public bool SpoilageDetected { get; set; }
    
    [FirestoreProperty]
    public Timestamp Timestamp { get; set; }
    
    [FirestoreProperty]
    public string TriggerType { get; set; } = "scheduled"; // "scheduled" sau "manual"
}