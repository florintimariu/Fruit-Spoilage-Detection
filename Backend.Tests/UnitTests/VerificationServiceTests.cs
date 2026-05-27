using Backend.Models;
using Backend.Services.Implementations;
using Backend.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

public class VerificationServiceTests
{
    private readonly Mock<IStepService> _stepServiceMock;
    private readonly Mock<IBlockchainService> _blockchainServiceMock;
    private readonly Mock<IHashingService> _hashingServiceMock;
    private readonly Mock<ILogger<VerificationService>> _loggerMock;
    private readonly VerificationService _sut;

    public VerificationServiceTests()
    {
        _stepServiceMock = new Mock<IStepService>();
        _blockchainServiceMock = new Mock<IBlockchainService>();
        _hashingServiceMock = new Mock<IHashingService>();
        _loggerMock = new Mock<ILogger<VerificationService>>();

        _sut = new VerificationService(
            _stepServiceMock.Object,
            _blockchainServiceMock.Object,
            _hashingServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task VerifyStepIntegrity_StepNotFound_ReturnsStepNotFound()
    {
        // Arrange
        _stepServiceMock
            .Setup(s => s.GetStepAsync("ship-1", "step-1"))
            .ReturnsAsync((Step?)null);

        // Act
        var result = await _sut.VerifyStepIntegrityAsync("ship-1", "step-1");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("STEP_NOT_FOUND");
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task VerifyStepIntegrity_StepNotCompleted_ReturnsNotCompleted()
    {
        // Arrange
        var step = new Step
        {
            StepId = "step-1",
            ShipmentId = "ship-1",
            IsCompleted = false
        };
        _stepServiceMock
            .Setup(s => s.GetStepAsync("ship-1", "step-1"))
            .ReturnsAsync(step);

        // Act
        var result = await _sut.VerifyStepIntegrityAsync("ship-1", "step-1");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("NOT_COMPLETED");
    }

    [Fact]
    public async Task VerifyStepIntegrity_StepCompletedButNotAnchored_ReturnsNotAnchored()
    {
        // Arrange
        var step = new Step
        {
            StepId = "step-1",
            ShipmentId = "ship-1",
            IsCompleted = true,
            DataHash = "0xabc123",
            TransactionHash = null // nu a fost ancorat
        };
        _stepServiceMock
            .Setup(s => s.GetStepAsync("ship-1", "step-1"))
            .ReturnsAsync(step);

        // Act
        var result = await _sut.VerifyStepIntegrityAsync("ship-1", "step-1");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("NOT_ANCHORED");
        result.StoredHash.Should().Be("0xabc123");
    }

    [Fact]
    public async Task VerifyStepIntegrity_HashesMatch_ReturnsValid()
    {
        // Arrange — scenariul VALID: hash recomputat = hash on-chain
        var step = CreateCompletedAnchoredStep();
        var matchingHash = new byte[] { 0x12, 0x34, 0xab, 0xcd };
        
        _stepServiceMock
            .Setup(s => s.GetStepAsync(step.ShipmentId, step.StepId))
            .ReturnsAsync(step);
        
        _hashingServiceMock
            .Setup(h => h.ComputeKeccak256(It.IsAny<object>()))
            .Returns(matchingHash);
        
        _hashingServiceMock
            .Setup(h => h.ToHexString(matchingHash))
            .Returns("0x1234abcd");

        _blockchainServiceMock
            .Setup(b => b.GetStepFromTransactionAsync(step.TransactionHash!))
            .ReturnsAsync(new OnChainStepData(
                step.ShipmentId, step.StepId, "Fresh", 
                matchingHash, 1234567890));

        // Act
        var result = await _sut.VerifyStepIntegrityAsync(step.ShipmentId, step.StepId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Status.Should().Be("VALID");
        result.StoredHash.Should().Be("0x1234abcd");
        result.OnChainHash.Should().Be("0x1234abcd");
        result.Message.Should().Contain("verified");
    }

    [Fact]
    public async Task VerifyStepIntegrity_HashesDiffer_ReturnsTampered()
    {
        // Arrange — scenariul TAMPERED: cineva a modificat Firestore
        var step = CreateCompletedAnchoredStep();
        var recomputedHash = new byte[] { 0x11, 0x11 };
        var onChainHash = new byte[] { 0x22, 0x22 };

        _stepServiceMock
            .Setup(s => s.GetStepAsync(step.ShipmentId, step.StepId))
            .ReturnsAsync(step);

        _hashingServiceMock
            .Setup(h => h.ComputeKeccak256(It.IsAny<object>()))
            .Returns(recomputedHash);

        _hashingServiceMock
            .Setup(h => h.ToHexString(recomputedHash))
            .Returns("0x1111");
        _hashingServiceMock
            .Setup(h => h.ToHexString(onChainHash))
            .Returns("0x2222");

        _blockchainServiceMock
            .Setup(b => b.GetStepFromTransactionAsync(step.TransactionHash!))
            .ReturnsAsync(new OnChainStepData(
                step.ShipmentId, step.StepId, "Fresh",
                onChainHash, 1234567890));

        // Act
        var result = await _sut.VerifyStepIntegrityAsync(step.ShipmentId, step.StepId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("TAMPERED");
        result.StoredHash.Should().Be("0x1111");
        result.OnChainHash.Should().Be("0x2222");
        result.Message.Should().Contain("modified");
    }

    [Fact]
    public async Task VerifyStepIntegrity_BlockchainReadFails_ReturnsBlockchainReadFailed()
    {
        // Arrange — scenariul cand citirea de pe blockchain esueaza
        var step = CreateCompletedAnchoredStep();
        var hash = new byte[] { 0xff };

        _stepServiceMock
            .Setup(s => s.GetStepAsync(step.ShipmentId, step.StepId))
            .ReturnsAsync(step);

        _hashingServiceMock
            .Setup(h => h.ComputeKeccak256(It.IsAny<object>()))
            .Returns(hash);
        _hashingServiceMock
            .Setup(h => h.ToHexString(hash))
            .Returns("0xff");

        _blockchainServiceMock
            .Setup(b => b.GetStepFromTransactionAsync(step.TransactionHash!))
            .ReturnsAsync((OnChainStepData?)null);

        // Act
        var result = await _sut.VerifyStepIntegrityAsync(step.ShipmentId, step.StepId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("BLOCKCHAIN_READ_FAILED");
        result.TransactionHash.Should().Be(step.TransactionHash);
    }

    // Helper care construieste un Step "valid" pentru testele de mai sus
    private static Step CreateCompletedAnchoredStep()
    {
        return new Step
        {
            StepId = "step-1",
            ShipmentId = "ship-1",
            Type = "Transport",
            LocationName = "Warehouse A",
            OperatorName = "John Doe",
            IsCompleted = true,
            MinTemp = 2.0,
            MaxTemp = 6.0,
            AvgTemp = 4.0,
            MinHumidity = 60,
            MaxHumidity = 80,
            AvgHumidity = 70,
            ReadingsCount = 60,
            AiStatusAtCompletion = "Fresh",
            DataHash = "0x1234abcd",
            TransactionHash = "0xtx-hash-deadbeef"
        };
    }
}