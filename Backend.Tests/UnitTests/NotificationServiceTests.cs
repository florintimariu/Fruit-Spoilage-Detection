using Backend.Models;
using Backend.Services.Implementations;
using Backend.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests;

public class NotificationServiceTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<IOrganizationService> _orgServiceMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _orgServiceMock = new Mock<IOrganizationService>();
        _loggerMock = new Mock<ILogger<NotificationService>>();

        _sut = new NotificationService(
            _userServiceMock.Object,
            _orgServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task SendToUser_UserNotFound_SkipsSilently()
    {
        // Arrange
        _userServiceMock
            .Setup(u => u.GetUserAsync("non-existent"))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.SendToUserAsync("non-existent", "Title", "Body");

        // Assert — nu arunca exceptie, doar log si exit
        await act.Should().NotThrowAsync();
        _userServiceMock.Verify(u => u.GetUserAsync("non-existent"), Times.Once);
    }

    [Fact]
    public async Task SendToUser_UserWithoutFcmToken_SkipsSilently()
    {
        // Arrange — user exista dar fara token FCM (n-a deschis app-ul niciodata)
        var user = new User { UserId = "user-1", Email = "test@test.com", FcmToken = null };
        _userServiceMock
            .Setup(u => u.GetUserAsync("user-1"))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.SendToUserAsync("user-1", "Title", "Body");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToUser_UserWithEmptyFcmToken_SkipsSilently()
    {
        // Arrange
        var user = new User { UserId = "user-1", FcmToken = "" };
        _userServiceMock
            .Setup(u => u.GetUserAsync("user-1"))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.SendToUserAsync("user-1", "Title", "Body");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendToOrganization_OrgNotFound_DoesNothing()
    {
        // Arrange
        _orgServiceMock
            .Setup(o => o.GetOrganizationAsync("non-existent"))
            .ReturnsAsync((Organization?)null);

        // Act
        await _sut.SendToOrganizationAsync("non-existent", "Title", "Body");

        // Assert — nu incearca sa trimita la nimeni
        _userServiceMock.Verify(u => u.GetUserAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendToOrganization_OrgWithMembers_AttemptsToSendToEach()
    {
        // Arrange — organization cu 3 membri, fara FCM tokens
        var org = new Organization
        {
            OrganizationId = "org-1",
            Members = new List<Member>
            {
                new() { UserId = "user-1" },
                new() { UserId = "user-2" },
                new() { UserId = "user-3" }
            }
        };
        _orgServiceMock
            .Setup(o => o.GetOrganizationAsync("org-1"))
            .ReturnsAsync(org);
        // Nu setup-am FCM tokens — toti vor fi skipped, dar GetUserAsync apelat
        _userServiceMock
            .Setup(u => u.GetUserAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act
        await _sut.SendToOrganizationAsync("org-1", "Compromise Alert", "Shipment X compromised");

        // Assert — verifica ca a iterat prin toti membrii
        _userServiceMock.Verify(u => u.GetUserAsync("user-1"), Times.Once);
        _userServiceMock.Verify(u => u.GetUserAsync("user-2"), Times.Once);
        _userServiceMock.Verify(u => u.GetUserAsync("user-3"), Times.Once);
    }

    [Fact]
    public async Task SendToOrganization_OrgWithNoMembers_DoesNothing()
    {
        // Arrange
        var org = new Organization { OrganizationId = "org-1", Members = new List<Member>() };
        _orgServiceMock
            .Setup(o => o.GetOrganizationAsync("org-1"))
            .ReturnsAsync(org);

        // Act
        await _sut.SendToOrganizationAsync("org-1", "Title", "Body");

        // Assert
        _userServiceMock.Verify(u => u.GetUserAsync(It.IsAny<string>()), Times.Never);
    }
}