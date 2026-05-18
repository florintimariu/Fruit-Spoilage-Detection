using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class User
{
    [FirestoreProperty]
    public string UserId { get; set; } = string.Empty;  // Firebase uid
    
    [FirestoreProperty]
    public string Email { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string DisplayName { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string? PhotoUrl { get; set; }
    
    [FirestoreProperty]
    public List<string> OrganizationIds { get; set; } = new();
    
    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
    
    [FirestoreProperty]
    public Timestamp? LastLoginAt { get; set; }
    
    [FirestoreProperty]
    public string? FcmToken { get; set; }
}