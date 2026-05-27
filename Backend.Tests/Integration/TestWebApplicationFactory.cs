using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Integration;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Variabila de mediu = FirestoreDb se conecteaza la emulator
        Environment.SetEnvironmentVariable("FIRESTORE_EMULATOR_HOST",
            FirestoreEmulatorFixture.EmulatorHost);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testConfig = new Dictionary<string, string?>
            {
                ["Firebase:ProjectId"] = FirestoreEmulatorFixture.ProjectId,
                ["Firebase:CredentialsPath"] = "fake-credentials.json",
                ["Ethereum:ContractAddress"] = "0x0000000000000000000000000000000000000000",
                ["Ethereum:RpcUrl"] = "http://localhost:8545",
                ["Ethereum:PrivateKey"] = 
                    "0x0000000000000000000000000000000000000000000000000000000000000001"
            };
            config.AddInMemoryCollection(testConfig);
        });
    }
}