using Google.Cloud.Firestore;

namespace Backend.Models;

[FirestoreData]
public class Organization
{
    [FirestoreProperty]
    public string OrganizationId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Name { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string Description { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public string CreatedByUserId { get; set; } = string.Empty;
    
    [FirestoreProperty]
    public Timestamp CreatedAt { get; set; }
    
    [FirestoreProperty]
    public List<Member> Members { get; set; } = new();
}