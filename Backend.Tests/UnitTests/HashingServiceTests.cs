using Backend.Services.Implementations;
using FluentAssertions;

namespace Backend.Tests.UnitTests;

public class HashingServiceTests
{
    private readonly HashingService _sut;

    public HashingServiceTests()
    {
        _sut = new HashingService();
    }

    [Fact]
    public void ComputeKeccak256_SameInput_ProducesSameHash()
    {
        // Arrange — Hash determinist: aceleasi date trebuie sa produca acelasi hash
        var data = new { ShipmentId = "s1", StepId = "st1", AvgTemp = 4.5 };

        // Act
        var hash1 = _sut.ComputeKeccak256(data);
        var hash2 = _sut.ComputeKeccak256(data);

        // Assert
        hash1.Should().BeEquivalentTo(hash2);
        hash1.Length.Should().Be(32); // Keccak256 produce 32 bytes (256 biti)
    }

    [Fact]
    public void ComputeKeccak256_DifferentInputs_ProducesDifferentHashes()
    {
        // Arrange
        var data1 = new { ShipmentId = "s1", AvgTemp = 4.5 };
        var data2 = new { ShipmentId = "s1", AvgTemp = 4.6 }; // doar o diferenta minima

        // Act
        var hash1 = _sut.ComputeKeccak256(data1);
        var hash2 = _sut.ComputeKeccak256(data2);

        // Assert — orice modificare a datelor trebuie sa schimbe hash-ul (avalanche effect)
        hash1.Should().NotBeEquivalentTo(hash2);
    }

    [Fact]
    public void ToHexString_ValidBytes_ReturnsLowercaseHexWith0xPrefix()
    {
        // Arrange
        var bytes = new byte[] { 0xAB, 0xCD, 0x12, 0x34 };

        // Act
        var result = _sut.ToHexString(bytes);

        // Assert
        result.Should().Be("0xabcd1234");
    }

    [Fact]
    public void ToHexString_EmptyBytes_ReturnsOnlyPrefix()
    {
        // Arrange
        var bytes = Array.Empty<byte>();

        // Act
        var result = _sut.ToHexString(bytes);

        // Assert
        result.Should().Be("0x");
    }

    [Fact]
    public void HashRoundTrip_CanReproduceSameHexHash()
    {
        // Arrange — Simuleaza ce face VerificationService: hash + hex
        var stepData = new
        {
            ShipmentId = "shipment-123",
            StepId = "step-456",
            AvgTemp = 5.0,
            ReadingsCount = 60
        };

        // Act
        var bytes = _sut.ComputeKeccak256(stepData);
        var hex1 = _sut.ToHexString(bytes);
        
        // Recompute pentru verificare integritate
        var bytes2 = _sut.ComputeKeccak256(stepData);
        var hex2 = _sut.ToHexString(bytes2);

        // Assert
        hex1.Should().Be(hex2);
        hex1.Should().StartWith("0x");
        hex1.Length.Should().Be(66); // "0x" + 64 hex chars (32 bytes * 2)
    }
}