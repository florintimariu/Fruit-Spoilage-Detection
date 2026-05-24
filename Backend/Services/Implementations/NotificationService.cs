using FirebaseAdmin.Messaging;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUserService _userService;
    private readonly IOrganizationService _orgService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IUserService userService,
        IOrganizationService orgService,
        ILogger<NotificationService> logger)
    {
        _userService = userService;
        _orgService = orgService;
        _logger = logger;
    }

    public async Task SendToUserAsync(
        string userId,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        var user = await _userService.GetUserAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.FcmToken))
        {
            _logger.LogInformation("User {UserId} has no FCM token, skipping", userId);
            return;
        }

        var message = new Message
        {
            Token = user.FcmToken,
            Notification = new Notification
            {
                Title = title,
                Body = body
            },
            Data = data ?? new Dictionary<string, string>()
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Sent notification to {UserId}: {Response}", userId, response);
        }
        catch (FirebaseMessagingException ex)
        {
            _logger.LogWarning("Failed to send to {UserId}: {Message}", userId, ex.Message);
        }
    }

    public async Task SendToOrganizationAsync(
        string organizationId,
        string title,
        string body,
        Dictionary<string, string>? data = null)
    {
        var org = await _orgService.GetOrganizationAsync(organizationId);
        if (org == null) return;

        foreach (var member in org.Members)
        {
            await SendToUserAsync(member.UserId, title, body, data);
        }
    }
}