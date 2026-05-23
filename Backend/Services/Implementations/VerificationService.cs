using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class VerificationService : IVerificationService
{
    private readonly IStepService _stepService;
    private readonly IBlockchainService _blockchainService;
    private readonly IHashingService _hashingService;
    private readonly ILogger<VerificationService> _logger;

    public VerificationService(
        IStepService stepService,
        IBlockchainService blockchainService,
        IHashingService hashingService,
        ILogger<VerificationService> logger)
    {
        _stepService = stepService;
        _blockchainService = blockchainService;
        _hashingService = hashingService;
        _logger = logger;
    }

    public async Task<VerificationResult> VerifyStepIntegrityAsync(string shipmentId, string stepId)
    {
        var step = await _stepService.GetStepAsync(shipmentId, stepId);
        if (step == null)
        {
            return new VerificationResult(false, "STEP_NOT_FOUND", null, null, null, 
                "Step not found");
        }

        if (!step.IsCompleted)
        {
            return new VerificationResult(false, "NOT_COMPLETED", null, null, null,
                "Step is not completed yet, no hash anchored");
        }

        if (string.IsNullOrEmpty(step.TransactionHash))
        {
            return new VerificationResult(false, "NOT_ANCHORED", step.DataHash, null, null,
                "Step was completed but not anchored on blockchain");
        }

        // 1. Recalculeaza hash-ul din datele curente din Firestore
        var recomputedHash = _hashingService.ComputeKeccak256(new
        {
            ShipmentId = step.ShipmentId,
            StepId = step.StepId,
            Type = step.Type,
            LocationName = step.LocationName,
            OperatorName = step.OperatorName,
            MinTemp = step.MinTemp,
            MaxTemp = step.MaxTemp,
            AvgTemp = step.AvgTemp,
            MinHumidity = step.MinHumidity,
            MaxHumidity = step.MaxHumidity,
            AvgHumidity = step.AvgHumidity,
            ReadingsCount = step.ReadingsCount,
            AiStatus = step.AiStatusAtCompletion
        });
        var recomputedHashHex = _hashingService.ToHexString(recomputedHash);

        // 2. Citeste hash-ul de pe blockchain
        var onChainData = await _blockchainService.GetStepFromTransactionAsync(step.TransactionHash);
        if (onChainData == null)
        {
            return new VerificationResult(false, "BLOCKCHAIN_READ_FAILED", 
                recomputedHashHex, null, step.TransactionHash,
                "Could not read data from blockchain");
        }

        var onChainHashHex = _hashingService.ToHexString(onChainData.DataHash);

        // 3. Compara
        var isValid = recomputedHashHex.Equals(onChainHashHex, StringComparison.OrdinalIgnoreCase);

        if (isValid)
        {
            return new VerificationResult(true, "VALID", 
                recomputedHashHex, onChainHashHex, step.TransactionHash,
                "Data integrity verified: Firestore data matches blockchain anchor");
        }
        else
        {
            _logger.LogWarning(
                "INTEGRITY VIOLATION for step {StepId}: stored={Stored}, onchain={OnChain}",
                stepId, recomputedHashHex, onChainHashHex);
            return new VerificationResult(false, "TAMPERED", 
                recomputedHashHex, onChainHashHex, step.TransactionHash,
                "WARNING: Data has been modified! Firestore data does not match blockchain anchor");
        }
    }
}