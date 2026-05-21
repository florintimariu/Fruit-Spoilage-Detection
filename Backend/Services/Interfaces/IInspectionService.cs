using Backend.Models;
using Backend.Common.Enums;

namespace Backend.Services.Interfaces;

public interface IInspectionService
{
    Task<AiInspection> CreateInspectionAsync(
        string shipmentId,
        string? stepId,
        string imageUrl,
        string maskUrl,
        AiVerdict verdict,
        double spoilagePercent,
        string triggerType);
    
    Task<List<AiInspection>> GetInspectionsForShipmentAsync(string shipmentId);
    Task<AiInspection?> GetInspectionAsync(string shipmentId, string inspectionId);
}