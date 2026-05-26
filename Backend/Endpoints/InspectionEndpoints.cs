using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Endpoints;

public static class InspectionEndpoints
{
    public static void MapInspectionEndpoints(this WebApplication app)
    {
        // RPI trimite rezultatul inspectiei AI
        app.MapPost("/api/shipments/{shipmentId}/inspections", async (
            string shipmentId,
            CreateInspectionRequest request,
            HttpContext ctx,
            IInspectionService inspectionService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanModifyShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            if (!Enum.TryParse<AiVerdict>(request.Verdict, true, out var verdict))
                return Results.BadRequest(new { error = "Invalid verdict" });

            var inspection = await inspectionService.CreateInspectionAsync(
                shipmentId,
                request.StepId,
                request.ImageUrl,
                request.MaskUrl,
                verdict,
                request.SpoilagePercent,
                request.TriggerType ?? "manual");

            return Results.Created(
                $"/api/shipments/{shipmentId}/inspections/{inspection.InspectionId}",
                inspection);
        });

        // Listare inspectii pentru un shipment
        app.MapGet("/api/shipments/{shipmentId}/inspections", async (
            string shipmentId,
            HttpContext ctx,
            IInspectionService inspectionService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var inspections = await inspectionService.GetInspectionsForShipmentAsync(shipmentId);
            return Results.Ok(inspections);
        });

        // Detalii o inspectie
        app.MapGet("/api/shipments/{shipmentId}/inspections/{inspectionId}", async (
            string shipmentId,
            string inspectionId,
            HttpContext ctx,
            IInspectionService inspectionService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var inspection = await inspectionService.GetInspectionAsync(shipmentId, inspectionId);
            return inspection != null ? Results.Ok(inspection) : Results.NotFound();
        });
    }
}

public record CreateInspectionRequest(
    string? StepId,
    string ImageUrl,
    string MaskUrl,
    string Verdict,
    double SpoilagePercent,
    string? TriggerType);