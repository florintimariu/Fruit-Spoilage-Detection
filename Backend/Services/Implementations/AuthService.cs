using FirebaseAdmin.Auth;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ILogger<AuthService> _logger;
    private readonly IMemoryCache _cache;

    public AuthService(ILogger<AuthService> logger, IMemoryCache cache)
    {
        _logger = logger;
        _cache = cache;
    }

    public async Task<UserInfo?> VerifyTokenAndGetUserAsync(string idToken)
    {
        // Cache key bazat pe primele 20 caractere ale tokenului (unic per token)
        var cacheKey = $"firebase_token_{idToken[..20]}";

        if (_cache.TryGetValue(cacheKey, out UserInfo? cachedUserInfo))
        {
            return cachedUserInfo;
        }

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);

            // Extrage info din claims
            decoded.Claims.TryGetValue("email", out var emailObj);
            decoded.Claims.TryGetValue("name", out var nameObj);
            
            var email = emailObj?.ToString() ?? "";
            var displayName = nameObj?.ToString();

            var userInfo = new UserInfo(decoded.Uid, email, displayName);

            var expiry = DateTimeOffset.FromUnixTimeSeconds(decoded.ExpirationTimeSeconds);
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = expiry
            };

            _cache.Set(cacheKey, userInfo, cacheOptions);
            return userInfo;
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