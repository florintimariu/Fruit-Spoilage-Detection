using Backend.Services.Interfaces;

namespace Backend.Middleware;

public class FirebaseAuthMiddleware
{
    private readonly RequestDelegate _next;

    public FirebaseAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService, IUserService userService)
    {
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health") || path.StartsWith("/swagger") || path.StartsWith("/dev/"))
        {
            await _next(context);
            return;
        }

        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Missing or invalid Authorization header");
            return;
        }

        var token = authHeader.Substring("Bearer ".Length);
        var userInfo = await authService.VerifyTokenAndGetUserAsync(token);

        if (userInfo == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid token");
            return;
        }

        // Auto-create user dacă nu exista
        await userService.CreateOrGetUserAsync(userInfo.UserId, userInfo.Email, userInfo.DisplayName);
        await userService.UpdateLastLoginAsync(userInfo.UserId);

        context.Items["UserId"] = userInfo.UserId;
        context.Items["UserEmail"] = userInfo.Email;
        await _next(context);
    }
}