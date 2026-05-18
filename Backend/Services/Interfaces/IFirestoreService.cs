namespace Backend.Services.Interfaces;

public interface IFirestoreService
{
    Task<T?> GetDocumentAsync<T>(string collection, string documentId) where T : class;
    Task<List<T>> GetCollectionAsync<T>(string collection) where T : class;
    Task<string> AddDocumentAsync<T>(string collection, T data) where T : class;
    Task SetDocumentAsync<T>(string collection, string documentId, T data) where T : class;
    Task UpdateDocumentAsync(string collection, string documentId, Dictionary<string, object> updates);
    Task DeleteDocumentAsync(string collection, string documentId);
    Task<bool> DocumentExistsAsync(string collection, string documentId);
}