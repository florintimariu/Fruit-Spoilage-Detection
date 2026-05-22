using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Backend.Contracts;

[Event("StepLogged")]
public class StepLoggedEvent : IEventDTO
{
    [Parameter("string", "shipmentId", 1, false)]
    public string ShipmentId { get; set; } = string.Empty;

    [Parameter("string", "stepId", 2, false)]
    public string StepId { get; set; } = string.Empty;

    [Parameter("string", "aiStatus", 3, false)]
    public string AiStatus { get; set; } = string.Empty;

    [Parameter("bytes32", "dataHash", 4, false)]
    public byte[] DataHash { get; set; } = new byte[32];

    [Parameter("uint256", "timestamp", 5, false)]
    public System.Numerics.BigInteger Timestamp { get; set; }
}