using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Member
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Role { get; set; } = "Viewer";
    
    [FirestoreProperty]
    public Timestamp JoinedAt { get; set; }
}