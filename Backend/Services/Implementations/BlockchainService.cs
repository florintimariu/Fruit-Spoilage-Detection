using Nethereum.Web3;
using Backend.Contracts;
using Backend.Services.Interfaces;
using Nethereum.Contracts;

namespace Backend.Services.Implementations;

public class BlockchainService : IBlockchainService
{
    private readonly Web3 _web3;
    private readonly string _contractAddress;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(
        Web3 web3, 
        IConfiguration config,
        ILogger<BlockchainService> logger)
    {
        _web3 = web3;
        _contractAddress = config["Ethereum:ContractAddress"]!;
        _logger = logger;
    }

    public async Task<BlockchainAnchorResult> AnchorStepAsync(
        string shipmentId, 
        string stepId, 
        string aiStatus, 
        byte[] dataHash)
    {
        try
        {
            var logStepFunction = new LogStepFunction
            {
                ShipmentId = shipmentId,
                StepId = stepId,
                AiStatus = aiStatus,
                DataHash = dataHash
            };

            var handler = _web3.Eth.GetContractTransactionHandler<LogStepFunction>();
            var receipt = await handler.SendRequestAndWaitForReceiptAsync(
                _contractAddress, 
                logStepFunction);

            _logger.LogInformation(
                "Step {StepId} for shipment {ShipmentId} anchored on-chain: {TxHash}",
                stepId, shipmentId, receipt.TransactionHash);

            return new BlockchainAnchorResult(receipt.TransactionHash, true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to anchor step {StepId} for shipment {ShipmentId}", 
                stepId, shipmentId);
            return new BlockchainAnchorResult("", false, ex.Message);
        }
    }

    public async Task<OnChainStepData?> GetStepFromTransactionAsync(string transactionHash)
    {
        try
        {
            var receipt = await _web3.Eth.Transactions.GetTransactionReceipt
                .SendRequestAsync(transactionHash);

            if (receipt == null)
            {
                _logger.LogWarning("Transaction {TxHash} not found", transactionHash);
                return null;
            }

            var eventLogs = receipt.DecodeAllEvents<StepLoggedEvent>();
            var stepEvent = eventLogs.FirstOrDefault();

            if (stepEvent == null)
            {
                _logger.LogWarning("No StepLogged event in transaction {TxHash}", transactionHash);
                return null;
            }

            var ev = stepEvent.Event;
            return new OnChainStepData(
                ev.ShipmentId,
                ev.StepId,
                ev.AiStatus,
                ev.DataHash,
                ev.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read transaction {TxHash}", transactionHash);
            return null;
        }
    }
}