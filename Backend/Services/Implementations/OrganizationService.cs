using Google.Cloud.Firestore;
using Backend.Models;
using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Services.Implementations;

public class OrganizationService : IOrganizationService
{
    private const string CollectionName = "Organizations";
    private readonly IFirestoreService _firestore;
    private readonly IUserService _userService;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        IFirestoreService firestore, 
        IUserService userService,
        ILogger<OrganizationService> logger)
    {
        _firestore = firestore;
        _userService = userService;
        _logger = logger;
    }

    public async Task<Organization?> GetOrganizationAsync(string organizationId)
    {
        return await _firestore.GetDocumentAsync<Organization>(CollectionName, organizationId);
    }

    public async Task<List<Organization>> GetOrganizationsForUserAsync(string userId)
    {
        var user = await _userService.GetUserAsync(userId);
        if (user == null || !user.OrganizationIds.Any())
        {
            return new List<Organization>();
        }

        var organizations = new List<Organization>();
        foreach (var orgId in user.OrganizationIds)
        {
            var org = await GetOrganizationAsync(orgId);
            if (org != null)
            {
                organizations.Add(org);
            }
        }
        return organizations;
    }

    public async Task<Organization> CreateOrganizationAsync(string name, string description, string ownerId)
    {
        var organizationId = Guid.NewGuid().ToString();
        var organization = new Organization
        {
            OrganizationId = organizationId,
            Name = name,
            Description = description,
            CreatedByUserId = ownerId,
            CreatedAt = Timestamp.GetCurrentTimestamp(),
            Members = new List<Member>
            {
                new Member
                {
                    UserId = ownerId,
                    Role = OrganizationRole.Owner.ToString(),
                    JoinedAt = Timestamp.GetCurrentTimestamp()
                }
            }
        };

        await _firestore.SetDocumentAsync(CollectionName, organizationId, organization);
        await _userService.AddOrganizationToUserAsync(ownerId, organizationId);

        _logger.LogInformation("Created organization {OrgId} ({Name}) by user {UserId}", 
            organizationId, name, ownerId);
        return organization;
    }

    public async Task AddMemberAsync(string organizationId, string userId, OrganizationRole role)
    {
        var org = await GetOrganizationAsync(organizationId);
        if (org == null)
        {
            throw new InvalidOperationException($"Organization {organizationId} not found");
        }

        if (org.Members.Any(m => m.UserId == userId))
        {
            _logger.LogWarning("User {UserId} already member of {OrgId}", userId, organizationId);
            return;
        }

        org.Members.Add(new Member
        {
            UserId = userId,
            Role = role.ToString(),
            JoinedAt = Timestamp.GetCurrentTimestamp()
        });

        await _firestore.UpdateDocumentAsync(CollectionName, organizationId, 
            new Dictionary<string, object> { { "Members", org.Members } });

        await _userService.AddOrganizationToUserAsync(userId, organizationId);
    }

    public async Task RemoveMemberAsync(string organizationId, string userId)
    {
        var org = await GetOrganizationAsync(organizationId);
        if (org == null) return;

        org.Members.RemoveAll(m => m.UserId == userId);
        await _firestore.UpdateDocumentAsync(CollectionName, organizationId,
            new Dictionary<string, object> { { "Members", org.Members } });
    }

    public async Task<bool> IsUserMemberAsync(string organizationId, string userId)
    {
        var org = await GetOrganizationAsync(organizationId);
        return org?.Members.Any(m => m.UserId == userId) ?? false;
    }

    public async Task<OrganizationRole?> GetUserRoleAsync(string organizationId, string userId)
    {
        var org = await GetOrganizationAsync(organizationId);
        var member = org?.Members.FirstOrDefault(m => m.UserId == userId);
        if (member == null) return null;
        
        return Enum.TryParse<OrganizationRole>(member.Role, out var role) ? role : null;
    }
}