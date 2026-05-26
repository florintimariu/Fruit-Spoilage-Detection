using Google.Cloud.Firestore;
using Backend.Services.Interfaces;

namespace Backend.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        // Salveaza/actualizeaza FCM token pentru userul curent
        app.MapPost("/api/me/fcm-token", async (
            UpdateFcmTokenRequest request,
            HttpContext ctx,
            FirestoreDb db) =>
        {
            var userId = ctx.Items["UserId"] as string;
            if (userId == null) return Results.Unauthorized();

            await db.Collection("Users").Document(userId).UpdateAsync(
                new Dictionary<string, object> { { "FcmToken", request.FcmToken } });

            return Results.Ok(new { message = "FCM token updated" });
        });
    }
}

public record UpdateFcmTokenRequest(string FcmToken);