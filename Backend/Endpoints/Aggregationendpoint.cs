using Backend.Services.Interfaces;

namespace Backend.Endpoints;

public static class AggregationEndpoints
{
    public static void MapAggregationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/organizations/{organizationId}/stats");

        // Overview pe o perioada custom
        group.MapGet("/overview", async (
            string organizationId,
            string? period,        // "day", "week", "month" sau custom cu from/to
            DateTime? from,
            DateTime? to,
            HttpContext ctx,
            IAggregationService aggService,
            IOrganizationService orgService) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            var isMember = await orgService.IsUserMemberAsync(organizationId, userId);
            if (!isMember) return Results.StatusCode(403);

            // Determina intervalul
            DateTime periodFrom;
            DateTime periodTo = DateTime.UtcNow;

            if (period != null)
            {
                periodFrom = period.ToLower() switch
                {
                    "day" => DateTime.UtcNow.AddDays(-1),
                    "week" => DateTime.UtcNow.AddDays(-7),
                    "month" => DateTime.UtcNow.AddMonths(-1),
                    _ => DateTime.UtcNow.AddDays(-7)
                };
            }
            else if (from.HasValue && to.HasValue)
            {
                periodFrom = from.Value;
                periodTo = to.Value;
            }
            else
            {
                // Default: ultima saptamana
                periodFrom = DateTime.UtcNow.AddDays(-7);
            }

            var stats = await aggService.GetOverviewAsync(organizationId, periodFrom, periodTo);
            return Results.Ok(stats);
        });
    }
}