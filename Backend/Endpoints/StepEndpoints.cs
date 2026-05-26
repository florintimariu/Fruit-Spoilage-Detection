using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Endpoints;

public static class StepEndpoints
{
    public static void MapStepEndpoints(this WebApplication app)
    {
        // Listare steps pentru un shipment
        app.MapGet("/api/shipments/{shipmentId}/steps", async (
            string shipmentId,
            HttpContext ctx,
            IStepService stepService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var steps = await stepService.GetStepsForShipmentAsync(shipmentId);
            return Results.Ok(steps);
        });

        // Start step nou
        app.MapPost("/api/shipments/{shipmentId}/steps", async (
            string shipmentId,
            StartStepRequest request,
            HttpContext ctx,
            IStepService stepService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanModifyShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            if (!Enum.TryParse<StepType>(request.Type, true, out var stepType))
                return Results.BadRequest(new { error = "Invalid step type" });

            var step = await stepService.StartStepAsync(
                shipmentId,
                stepType,
                request.LocationName,
                request.OperatorName);

            return Results.Created(
                $"/api/shipments/{shipmentId}/steps/{step.StepId}", step);
        });

        // Complete step (declanseaza blockchain anchoring)
        app.MapPost("/api/shipments/{shipmentId}/steps/{stepId}/complete", async (
            string shipmentId,
            string stepId,
            CompleteStepRequest request,
            HttpContext ctx,
            IStepService stepService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanModifyShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var result = await stepService.CompleteStepAsync(
                shipmentId, stepId, request.AiStatus);

            if (result.Step == null)
                return Results.NotFound(new { error = result.ErrorMessage });

            return Results.Ok(new
            {
                step = result.Step,
                transactionHash = result.TransactionHash,
                anchoringSucceeded = result.AnchoringSucceeded,
                errorMessage = result.ErrorMessage
            });
        });
    }
}

public record StartStepRequest(string Type, string LocationName, string OperatorName);
public record CompleteStepRequest(string AiStatus);