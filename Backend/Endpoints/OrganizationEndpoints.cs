using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Endpoints;

public static class OrganizationEndpoints
{
    public static void MapOrganizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizations");

        // Listare organizatii ale userului curent
        group.MapGet("/", async (HttpContext ctx, IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var orgs = await orgService.GetOrganizationsForUserAsync(userId);
            return Results.Ok(orgs);
        });

        // Detalii organizatie
        group.MapGet("/{organizationId}", async (
            string organizationId,
            HttpContext ctx,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var isMember = await orgService.IsUserMemberAsync(organizationId, userId);
            if (!isMember) return Results.StatusCode(403);

            var org = await orgService.GetOrganizationAsync(organizationId);
            return org != null ? Results.Ok(org) : Results.NotFound();
        });

        // Creare organizatie noua
        group.MapPost("/", async (
            CreateOrganizationRequest request,
            HttpContext ctx,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name is required" });

            var org = await orgService.CreateOrganizationAsync(
                request.Name,
                request.Description ?? "",
                userId);

            return Results.Created($"/api/organizations/{org.OrganizationId}", org);
        });

        // Adaugare membru
        group.MapPost("/{organizationId}/members", async (
            string organizationId,
            AddMemberRequest request,
            HttpContext ctx,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            // Doar Owner poate adauga membri
            var role = await orgService.GetUserRoleAsync(organizationId, userId);
            if (role != OrganizationRole.Owner)
                return Results.StatusCode(403);

            if (!Enum.TryParse<OrganizationRole>(request.Role, true, out var newMemberRole))
                return Results.BadRequest(new { error = "Invalid role" });

            await orgService.AddMemberAsync(organizationId, request.UserId, newMemberRole);
            return Results.Ok(new { message = "Member added successfully" });
        });

        // Stergere membru
        group.MapDelete("/{organizationId}/members/{memberUserId}", async (
            string organizationId,
            string memberUserId,
            HttpContext ctx,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var role = await orgService.GetUserRoleAsync(organizationId, userId);
            if (role != OrganizationRole.Owner)
                return Results.StatusCode(403);

            await orgService.RemoveMemberAsync(organizationId, memberUserId);
            return Results.Ok(new { message = "Member removed successfully" });
        });

        // Adaugare membru pe baza de email
        group.MapPost("/{organizationId}/members/by-email", async (
            string organizationId,
            AddMemberByEmailRequest request,
            HttpContext ctx,
            IOrganizationService orgService,
            IUserService userService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var role = await orgService.GetUserRoleAsync(organizationId, userId);
            if (role != OrganizationRole.Owner)
                return Results.StatusCode(403);

            if (!Enum.TryParse<OrganizationRole>(request.Role, true, out var newMemberRole))
                return Results.BadRequest(new { error = "Invalid role" });

            var targetUser = await userService.GetUserByEmailAsync(request.Email);
            if (targetUser == null)
                return Results.NotFound(new { error = "No user found with this email" });

            await orgService.AddMemberAsync(organizationId, targetUser.UserId, newMemberRole);
            return Results.Ok(new { 
                message = "Member added successfully",
                userId = targetUser.UserId,
                email = targetUser.Email
            });
        });
    }
}

// DTOs
public record CreateOrganizationRequest(string Name, string? Description);
public record AddMemberRequest(string UserId, string Role);
public record AddMemberByEmailRequest(string Email, string Role);