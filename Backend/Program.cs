using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Web3;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Util;
using System.Text;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);

// Firebase setup
string filepath = "firebase-service-account-key.json";
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filepath);
string projectId = "florin-timariu-licenta";
builder.Services.AddSingleton<FirestoreDb>(FirestoreDb.Create(projectId));

// Ethereum setup
string contractAddress = builder.Configuration["Ethereum:ContractAddress"]!;
string rpcUrl = builder.Configuration["Ethereum:RpcUrl"]!;
string privateKey = builder.Configuration["Ethereum:PrivateKey"]!;

var web3 = new Web3(rpcUrl);
var account = new Nethereum.Web3.Accounts.Account(privateKey);
web3.TransactionManager.UseLegacyAsDefault = true; // For Sepolia

var app = builder.Build();

app.MapPost("/api/shipments/{shipmentId}/steps", async (
    string shipmentId,
    [FromBody] ShipmentStep stepData,
    FirestoreDb db) =>
{
    try
    {
        // 1. Compute hash of the stepData (serialize to JSON or use a canonical representation)
        string json = System.Text.Json.JsonSerializer.Serialize(stepData);
        byte[] hashBytes = Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(json));
        // Alternatively use SHA256 if you prefer, but Ethereum uses Keccak256 for bytes32

        // 2. Send transaction to smart contract
        var logStepFunction = new LogStepFunction
        {
            ShipmentId = shipmentId,
            StepId = stepData.StepId,
            AiStatus = stepData.AiStatus,
            DataHash = hashBytes
        };

        // Build web3 with account for signing
        var web3WithAccount = new Web3(account, rpcUrl);

        var handler = web3WithAccount.Eth.GetContractTransactionHandler<LogStepFunction>();
        var transactionReceipt = await handler.SendRequestAndWaitForReceiptAsync(
            contractAddress,
            logStepFunction
        );

        Console.WriteLine($"Transaction hash: {transactionReceipt.TransactionHash}");

        // 3. Write to Firestore
        CollectionReference stepsCollection = db.Collection("Shipments")
                                                .Document(shipmentId)
                                                .Collection("Steps");
        DocumentReference newDoc = await stepsCollection.AddAsync(stepData);

        return Results.Ok(new
        {
            Message = "Step successfully written to Firestore and anchored on-chain!",
            DocumentId = newDoc.Id,
            TransactionHash = transactionReceipt.TransactionHash
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Failed: {ex.Message}");
    }
});

app.Run();