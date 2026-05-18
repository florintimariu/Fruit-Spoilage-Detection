using Google.Cloud.Firestore;

// The [FirestoreData] attribute tells the SDK this class can be saved to the database
[FirestoreData]
public class ShipmentStep
{
    // [FirestoreProperty] tells the SDK to save this specific field
    [FirestoreProperty]
    public required string StepId { get; set; }

    [FirestoreProperty]
    public required DateTime StartTime { get; set; }

    [FirestoreProperty]
    public required double MinTemp { get; set; }

    [FirestoreProperty]
    public required double MaxTemp { get; set; }

    [FirestoreProperty]
    public required string AiStatus { get; set; }
}