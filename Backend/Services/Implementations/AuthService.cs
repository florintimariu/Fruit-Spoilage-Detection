using FirebaseAdmin.Auth;
using Backend.Services.Interfaces;

namespace Backend.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;

    public AuthService(ILogger<AuthService> logger)
    {
        _logger = logger;
    }

    public async Task<UserInfo?> VerifyTokenAndGetUserAsync(string idToken)
    {
        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            
            // Extrage info din claims
            decoded.Claims.TryGetValue("email", out var emailObj);
            decoded.Claims.TryGetValue("name", out var nameObj);
            
            var email = emailObj?.ToString() ?? "";
            var displayName = nameObj?.ToString();
            
            return new UserInfo(decoded.Uid, email, displayName);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning("Token verification failed: {Message}", ex.Message);
            return null;
        }
    }

    public async Task<bool> IsPlatformAdminAsync(string userId)
    {
        try
        {
            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(userId);
            return user.CustomClaims != null
                && user.CustomClaims.TryGetValue("platformAdmin", out var value)
                && value is bool b && b;
        }
        catch
        {
            return false;
        }
    }
}