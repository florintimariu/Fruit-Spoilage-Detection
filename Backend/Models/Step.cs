using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Step
{
    [FirestoreProperty]
    public string StepId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string ShipmentId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Type { get; set; } = string.Empty; // StepType enum
    
    [FirestoreProperty]
    public string LocationName { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string OperatorName { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public Timestamp StartTime { get; set; }
    
    [FirestoreProperty]
    public Timestamp? EndTime { get; set; }
    
    [FirestoreProperty]
    public bool IsCompleted { get; set; }
    
    // Agregări calculate la finalizarea step-ului
    [FirestoreProperty]
    public double? MinTemp { get; set; }
    
    [FirestoreProperty]
    public double? MaxTemp { get; set; }
    
    [FirestoreProperty]
    public double? AvgTemp { get; set; }
    
    [FirestoreProperty]
    public double? MinHumidity { get; set; }
    
    [FirestoreProperty]
    public double? MaxHumidity { get; set; }
    
    [FirestoreProperty]
    public double? AvgHumidity { get; set; }
    
    [FirestoreProperty]
    public int ReadingsCount { get; set; }
    
    [FirestoreProperty]
    public string? AiStatusAtCompletion { get; set; } // verdict ultim AI la momentul finalizării
    
    // Blockchain anchoring
    [FirestoreProperty]
    public string? DataHash { get; set; }       // hex string al hash-ului
    
    [FirestoreProperty]
    public string? TransactionHash { get; set; } // tx hash pe Sepolia
    
    [FirestoreProperty]
    public Timestamp? AnchoredAt { get; set; }   // când s-a făcut tranzacția
}