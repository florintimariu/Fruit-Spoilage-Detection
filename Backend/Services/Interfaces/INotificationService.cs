namespace Backend.Services.Interfaces;

public interface INotificationService
{
    Task SendToUserAsync(string userId, string title, string body, Dictionary<string, string>? data = null);
    Task SendToOrganizationAsync(string organizationId, string title, string body, Dictionary<string, string>? data = null);
}