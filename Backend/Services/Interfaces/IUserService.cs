using Backend.Models;

namespace Backend.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetUserAsync(string userId);
    Task<User> CreateOrGetUserAsync(string userId, string email, string? displayName);
    Task UpdateLastLoginAsync(string userId);
    Task AddOrganizationToUserAsync(string userId, string organizationId);
}