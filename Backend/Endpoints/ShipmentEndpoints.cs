using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Endpoints;

public static class ShipmentEndpoints
{
    public static void MapShipmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/shipments");

        // Listare shipments pentru o organizatie
        group.MapGet("/", async (
            string organizationId,
            HttpContext ctx,
            IShipmentService shipmentService,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var isMember = await orgService.IsUserMemberAsync(organizationId, userId);
            if (!isMember) return Results.StatusCode(403);

            var shipments = await shipmentService.GetShipmentsForOrganizationAsync(organizationId);
            return Results.Ok(shipments);
        });

        // Detalii shipment
        group.MapGet("/{shipmentId}", async (
            string shipmentId,
            HttpContext ctx,
            IShipmentService shipmentService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var shipment = await shipmentService.GetShipmentAsync(shipmentId);
            return shipment != null ? Results.Ok(shipment) : Results.NotFound();
        });

        // Creare shipment nou
        group.MapPost("/", async (
            CreateShipmentRequest request,
            HttpContext ctx,
            IShipmentService shipmentService,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var role = await orgService.GetUserRoleAsync(request.OrganizationId, userId);
            if (role != OrganizationRole.Owner && role != OrganizationRole.Operator)
                return Results.StatusCode(403);

            var shipment = await shipmentService.CreateShipmentAsync(
                request.OrganizationId,
                request.ProductName,
                request.ProductDescription ?? "",
                request.Origin,
                request.Destination,
                userId);

            return Results.Created($"/api/shipments/{shipment.ShipmentId}", shipment);
        });
    }
}

public record CreateShipmentRequest(
    string OrganizationId,
    string ProductName,
    string? ProductDescription,
    string Origin,
    string Destination);