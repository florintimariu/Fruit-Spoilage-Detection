using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

string filepath = "firebase-service-account-key.json";
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);

string projectId = "Florin-Timariu-Licenta";
builder.Services.AddSingleton<FirestoreDb>(FirestoreDb.Create(projectId));

var app = builder.Build();

app.MapPost("/api/shipments/{shipmentId}/steps", async (
    string shipmentId, 
    [FromBody] ShipmentStep stepData, 
    FirestoreDb db) =>
{
    try
    {
        CollectionReference stepsCollection = db.Collection("Shipments")
                                                .Document(shipmentId)
                                                .Collection("Steps");

        DocumentReference newDoc = await stepsCollection.AddAsync(stepData);

        return Results.Ok(new 
        { 
            Message = "Step successfully written to Firestore!", 
            DocumentId = newDoc.Id 
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed to write to database: {ex.Message}");
    }
});

app.Run();