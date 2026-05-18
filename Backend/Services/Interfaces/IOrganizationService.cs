using Backend.Models;
using Backend.Common.Enums;

namespace Backend.Services.Interfaces;

public interface IOrganizationService
{
    Task<Organization?> GetOrganizationAsync(string organizationId);
    Task<List<Organization>> GetOrganizationsForUserAsync(string userId);
    Task<Organization> CreateOrganizationAsync(string name, string description, string ownerId);
    Task AddMemberAsync(string organizationId, string userId, OrganizationRole role);
    Task RemoveMemberAsync(string organizationId, string userId);
    Task<bool> IsUserMemberAsync(string organizationId, string userId);
    Task<OrganizationRole?> GetUserRoleAsync(string organizationId, string userId);
}