using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

[Function("logStep")]
public class LogStepFunction : FunctionMessage
{
    [Parameter("string", "_shipmentId", 1)]
    public string ShipmentId { get; set; } = string.Empty;

    [Parameter("string", "_stepId", 2)]
    public string StepId { get; set; } = string.Empty;

    [Parameter("string", "_aiStatus", 3)]
    public string AiStatus { get; set; } = string.Empty;

    [Parameter("bytes32", "_dataHash", 4)]
    public byte[] DataHash { get; set; } = new byte[32];
}