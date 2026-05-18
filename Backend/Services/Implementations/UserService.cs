using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class UserService : IUserService
{
    private const string CollectionName = "Users";
    private readonly IFirestoreService _firestore;
    private readonly ILogger<UserService> _logger;

    public UserService(IFirestoreService firestore, ILogger<UserService> logger)
    {
        _firestore = firestore;
        _logger = logger;
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        return await _firestore.GetDocumentAsync<User>(CollectionName, userId);
    }

    public async Task<User> CreateOrGetUserAsync(string userId, string email, string? displayName)
    {
        var existing = await GetUserAsync(userId);
        if (existing != null)
        {
            return existing;
        }

        var newUser = new User
        {
            UserId = userId,
            Email = email,
            DisplayName = displayName ?? email.Split('@')[0],
            OrganizationIds = new List<string>(),
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            LastLoginAt = Timestamp.GetCurrentTimestamp()
        };

        await _firestore.SetDocumentAsync(CollectionName, userId, newUser);
        _logger.LogInformation("Created new user: {UserId} ({Email})", userId, email);
        return newUser;
    }

    public async Task UpdateLastLoginAsync(string userId)
    {
        await _firestore.UpdateDocumentAsync(CollectionName, userId, new Dictionary<string, object>
        {
            { "LastLoginAt", Timestamp.GetCurrentTimestamp() }
        });
    }

    public async Task AddOrganizationToUserAsync(string userId, string organizationId)
    {
        var user = await GetUserAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("Cannot add organization to non-existent user: {UserId}", userId);
            return;
        }

        if (!user.OrganizationIds.Contains(organizationId))
        {
            user.OrganizationIds.Add(organizationId);
            await _firestore.UpdateDocumentAsync(CollectionName, userId, new Dictionary<string, object>
            {
                { "OrganizationIds", user.OrganizationIds }
            });
        }
    }
}