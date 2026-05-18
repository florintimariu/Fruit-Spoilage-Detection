namespace Backend.Services.Interfaces;

public interface IAuthService
{
    Task<UserInfo?> VerifyTokenAndGetUserAsync(string idToken);
    Task<bool> IsPlatformAdminAsync(string userId);
}

public record UserInfo(string UserId, string Email, string? DisplayName);