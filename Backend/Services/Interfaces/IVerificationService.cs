namespace Backend.Services.Interfaces;

public interface IVerificationService
{
    Task<VerificationResult> VerifyStepIntegrityAsync(string shipmentId, string stepId);
}

public record VerificationResult(
    bool IsValid,
    string Status,
    string? StoredHash,
    string? OnChainHash,
    string? TransactionHash,
    string? Message);