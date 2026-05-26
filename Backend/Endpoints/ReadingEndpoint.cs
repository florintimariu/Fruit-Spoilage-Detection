using Backend.Services.Interfaces;

namespace Backend.Endpoints;

public static class ReadingEndpoints
{
    public static void MapReadingEndpoints(this WebApplication app)
    {
        // Batch upload de la RPI
        app.MapPost("/api/shipments/{shipmentId}/steps/{stepId}/readings/batch", async (
            string shipmentId,
            string stepId,
            ReadingsBatchRequest request,
            HttpContext ctx,
            IReadingService readingService,
            IStepService stepService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanModifyShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            // Verifica ca step-ul exista si nu e completed
            var step = await stepService.GetStepAsync(shipmentId, stepId);
            if (step == null)
                return Results.NotFound(new { error = "Step not found" });

            if (step.IsCompleted)
                return Results.BadRequest(new { error = "Cannot add readings to completed step" });

            var result = await readingService.ProcessBatchAsync(
                shipmentId,
                stepId,
                request.Readings,
                request.Location);

            return Results.Ok(result);
        });

        // Listare readings pentru un step
        app.MapGet("/api/shipments/{shipmentId}/steps/{stepId}/readings", async (
            string shipmentId,
            string stepId,
            HttpContext ctx,
            IReadingService readingService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var readings = await readingService.GetReadingsForStepAsync(shipmentId, stepId);
            return Results.Ok(readings);
        });

        // Listare locatii pentru un step (pentru track GPS)
        app.MapGet("/api/shipments/{shipmentId}/steps/{stepId}/locations", async (
            string shipmentId,
            string stepId,
            HttpContext ctx,
            IReadingService readingService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var locations = await readingService.GetLocationsForStepAsync(shipmentId, stepId);
            return Results.Ok(locations);
        });
    }
}

public record ReadingsBatchRequest(
    List<ReadingInput> Readings,
    LocationInput? Location);