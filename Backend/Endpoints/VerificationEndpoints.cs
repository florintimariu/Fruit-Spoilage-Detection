using Backend.Services.Interfaces;

namespace Backend.Endpoints;

public static class VerificationEndpoints
{
    public static void MapVerificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/shipments/{shipmentId}/steps/{stepId}/verify", async (
            string shipmentId,
            string stepId,
            HttpContext ctx,
            IVerificationService verificationService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanAccessShipmentAsync(userId, shipmentId))
                return Results.StatusCode(403);

            var result = await verificationService.VerifyStepIntegrityAsync(shipmentId, stepId);
            return Results.Ok(result);
        });
    }
}