namespace Backend.Services.Interfaces;

public interface IBlockchainService
{
    Task<BlockchainAnchorResult> AnchorStepAsync(
        string shipmentId, 
        string stepId, 
        string aiStatus, 
        byte[] dataHash);

        Task<OnChainStepData?> GetStepFromTransactionAsync(string transactionHash);

}

public record BlockchainAnchorResult(
    string TransactionHash, 
    bool Success, 
    string? ErrorMessage);

public record OnChainStepData(
    string ShipmentId,
    string StepId,
    string AiStatus,
    byte[] DataHash,
    System.Numerics.BigInteger Timestamp);