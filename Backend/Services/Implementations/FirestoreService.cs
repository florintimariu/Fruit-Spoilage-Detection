using Google.Cloud.Firestore;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class FirestoreService : IFirestoreService
{
    private readonly FirestoreDb _db;
    private readonly ILogger<FirestoreService> _logger;

    public FirestoreService(FirestoreDb db, ILogger<FirestoreService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<T?> GetDocumentAsync<T>(string collection, string documentId) where T : class
    {
        var snapshot = await _db.Collection(collection).Document(documentId).GetSnapshotAsync();
        return snapshot.Exists ? snapshot.ConvertTo<T>() : null;
    }

    public async Task<List<T>> GetCollectionAsync<T>(string collection) where T : class
    {
        var snapshot = await _db.Collection(collection).GetSnapshotAsync();
        return snapshot.Documents.Select(d => d.ConvertTo<T>()).ToList();
    }

    public async Task<string> AddDocumentAsync<T>(string collection, T data) where T : class
    {
        var docRef = await _db.Collection(collection).AddAsync(data);
        return docRef.Id;
    }

    public async Task SetDocumentAsync<T>(string collection, string documentId, T data) where T : class
    {
        await _db.Collection(collection).Document(documentId).SetAsync(data);
    }

    public async Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> updates)
    {
        await _db.Collection(collection).Document(documentId).UpdateAsync(updates);
    }

    public async Task DeleteDocumentAsync(string collection, string documentId)
    {
        await _db.Collection(collection).Document(documentId).DeleteAsync();
    }

    public async Task<bool> DocumentExistsAsync(string collection, string documentId)
    {
        var snapshot = await _db.Collection(collection).Document(documentId).GetSnapshotAsync();
        return snapshot.Exists;
    }
}