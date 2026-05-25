using System.Text;
using Nethereum.Util;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class HashingService : IHashingService
{
    public byte[] ComputeKeccak256<T>(T data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(data);
        return Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(json));
    }

    public string ToHexString(byte[] bytes)
    {
        return "0x" + Convert.ToHexString(bytes).ToLowerInvariant();
    }
}