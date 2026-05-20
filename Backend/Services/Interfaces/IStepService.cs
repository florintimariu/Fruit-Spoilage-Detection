using Backend.Models;
using Backend.Common.Enums;

namespace Backend.Services.Interfaces;

public interface IStepService
{
    Task<Step?> GetStepAsync(string shipmentId, string stepId);
    Task<List<Step>> GetStepsForShipmentAsync(string shipmentId);
    Task<Step> StartStepAsync(
        string shipmentId,
        StepType type,
        string locationName,
        string operatorName);
    Task<StepCompletionResult> CompleteStepAsync(
        string shipmentId,
        string stepId,
        string aiStatus);
}

public record StepCompletionResult(
    Step Step,
    string? TransactionHash,
    bool AnchoringSucceeded,
    string? ErrorMessage);