using Backend.Services.Interfaces;
using Backend.Common.Enums;

namespace Backend.Endpoints;

public static class SensorEndpoints
{
    public static void MapSensorEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/sensors");

        // Listare toti senzorii
        group.MapGet("/", async (
            HttpContext ctx,
            ISensorService sensorService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var sensors = await sensorService.GetAllSensorsAsync();
            return Results.Ok(sensors);
        });

        // Detalii senzor
        group.MapGet("/{ieee}", async (
            string ieee,
            HttpContext ctx,
            ISensorService sensorService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var sensor = await sensorService.GetSensorAsync(ieee);
            return sensor != null ? Results.Ok(sensor) : Results.NotFound();
        });

        // Inregistrare senzor nou
        group.MapPost("/", async (
            RegisterSensorRequest request,
            HttpContext ctx,
            ISensorService sensorService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!Enum.TryParse<SensorType>(request.SensorType, true, out var sensorType))
                return Results.BadRequest(new { error = "Invalid sensor type" });

            var sensor = await sensorService.RegisterSensorAsync(
                request.Ieee,
                request.LogicalId,
                request.DisplayName,
                sensorType,
                request.Unit);

            return Results.Created($"/api/sensors/{sensor.Ieee}", sensor);
        });

        // Asignare la shipment
        group.MapPost("/{ieee}/assign", async (
            string ieee,
            AssignSensorRequest request,
            HttpContext ctx,
            ISensorService sensorService,
            IShipmentAuthorizationService authz) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            if (!await authz.CanModifyShipmentAsync(userId, request.ShipmentId))
                return Results.StatusCode(403);

            var success = await sensorService.AssignToShipmentAsync(ieee, request.ShipmentId);
            return success ? Results.Ok() : Results.BadRequest();
        });
    }
}

public record RegisterSensorRequest(
    string Ieee,
    string LogicalId,
    string DisplayName,
    string SensorType,
    string Unit);

public record AssignSensorRequest(string ShipmentId);