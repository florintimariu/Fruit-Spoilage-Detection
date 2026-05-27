using Google.Cloud.Firestore;

namespace Backend.Tests.Integration;

public class FirestoreEmulatorFixture : IAsyncLifetime
{
    public const string ProjectId = "florin-timariu-licenta";
    public const string EmulatorHost = "localhost:8080";

    public FirestoreDb Firestore { get; private set; } = null!;

    public Task InitializeAsync()
    {
        // Variabila de mediu = clienti se conecteaza la emulator, nu la Firestore real
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST", EmulatorHost);

        // Creem o instanta FirestoreDb care va folosi emulatorul
        Firestore = new FirestoreDbBuilder
        {
            ProjectId = ProjectId,
            EmulatorDetection = Google.Api.Gax.EmulatorDetection.EmulatorOnly
        }.Build();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Curatam toate datele dupa terminarea tuturor testelor din clasa
        await ClearAllCollectionsAsync();
    }

    public async Task ClearAllCollectionsAsync()
    {
        var collections = new[] { "Shipments", "Organizations", "Users", "Sensors" };
        foreach (var name in collections)
        {
            var snapshot = await Firestore.Collection(name).GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                await doc.Reference.DeleteAsync();
            }
        }
    }
}