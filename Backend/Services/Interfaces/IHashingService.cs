namespace Backend.Services.Interfaces;

public interface IHashingService
{
    byte[] ComputeKeccak256<T>(T data);
    string ToHexString(byte[] bytes);
}