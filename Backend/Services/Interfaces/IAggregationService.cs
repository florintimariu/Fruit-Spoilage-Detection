namespace Backend.Services.Interfaces;

public interface IAggregationService
{
    Task<OverviewStats> GetOverviewAsync(
        string organizationId,
        DateTime from,
        DateTime to);
}

public record OverviewStats(
    int TotalShipments,
    int CompletedShipments,
    int InProgressShipments,
    int CompromisedShipments,
    int CreatedShipments,
    DateTime PeriodFrom,
    DateTime PeriodTo);