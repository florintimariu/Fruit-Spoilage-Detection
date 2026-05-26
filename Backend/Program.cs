using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Nethereum.Web3;
using Backend.Middleware;
using Backend.Services.Interfaces;
using Backend.Services.Implementations;
using Backend.Endpoints;

var builder = WebApplication.CreateBuilder(args);

var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]!;
var credentialsPath = builder.Configuration["Firebase:CredentialsPath"]!;

Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialsPath);

if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.GetApplicationDefault(),
        ProjectId = firebaseProjectId
    });
}

builder.Services.AddSingleton<FirestoreDb>(FirestoreDb.Create(firebaseProjectId));

// === Ethereum setup ===
var contractAddress = builder.Configuration["Ethereum:ContractAddress"]!;
var rpcUrl = builder.Configuration["Ethereum:RpcUrl"]!;
var privateKey = builder.Configuration["Ethereum:PrivateKey"]!;

var account = new Nethereum.Web3.Accounts.Account(privateKey);
var web3 = new Web3(account, rpcUrl);
builder.Services.AddSingleton(web3);

// === Services ===
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFirestoreService, FirestoreService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IBlockchainService, BlockchainService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IStepService, StepService>();
builder.Services.AddScoped<IShipmentAuthorizationService, ShipmentAuthorizationService>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IReadingService, ReadingService>();
builder.Services.AddScoped<IInspectionService, InspectionService>();
builder.Services.AddScoped<IAggregationService, AggregationService>();
builder.Services.AddScoped<IVerificationService, VerificationService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddMemoryCache();

// === API setup ===
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<FirebaseAuthMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/dev/get-token/{userId}", async (string userId) =>
{https://console.firebase.google.com/u/0/project/florin-timariu-licenta/authentication/users
    var customToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance
        .CreateCustomTokenAsync(userId);
    return Results.Ok(new { customToken });
});

app.MapGet("/api/me", (HttpContext ctx) =>
{
    var userId = ctx.Items["UserId"] as string;
    return Results.Ok(new { userId });
});

app.MapOrganizationEndpoints();
app.MapShipmentEndpoints();
app.MapStepEndpoints();
app.MapReadingEndpoints();
app.MapSensorEndpoints();
app.MapInspectionEndpoints();
app.MapAggregationEndpoints();
app.MapVerificationEndpoints();
app.MapNotificationEndpoints();

app.Run();

public partial class Program { }