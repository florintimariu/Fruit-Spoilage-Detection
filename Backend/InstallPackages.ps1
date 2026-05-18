# install-packages.ps1
# Script pentru instalarea tuturor pachetelor NuGet necesare backend-ului FreshLedger

Write-Host "Installing NuGet packages for FreshLedger Backend..." -ForegroundColor Cyan

# === Existing dependencies (probabil ai deja) ===
Write-Host "`nFirebase & Firestore..." -ForegroundColor Yellow
dotnet add package Google.Cloud.Firestore

Write-Host "`nNethereum (Blockchain)..." -ForegroundColor Yellow
dotnet add package Nethereum.Web3

# === Firebase Authentication ===
Write-Host "`nFirebase Admin SDK (Authentication)..." -ForegroundColor Yellow
dotnet add package FirebaseAdmin

# === JWT Bearer Authentication ===
Write-Host "`nASP.NET JWT Bearer Authentication..." -ForegroundColor Yellow
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer

# === Logging - Serilog ===
Write-Host "`nSerilog (structured logging)..." -ForegroundColor Yellow
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# === Validation ===
Write-Host "`nFluentValidation..." -ForegroundColor Yellow
dotnet add package FluentValidation
dotnet add package FluentValidation.AspNetCore
dotnet add package FluentValidation.DependencyInjectionExtensions

# === API Documentation (Swagger) ===
Write-Host "`nSwagger / OpenAPI..." -ForegroundColor Yellow
dotnet add package Swashbuckle.AspNetCore

# === Restore ===
Write-Host "`nRestoring all packages..." -ForegroundColor Yellow
dotnet restore

Write-Host "`nDone! All packages installed." -ForegroundColor Green
Write-Host "`nNext step: Verify the build with 'dotnet build'" -ForegroundColor Cyan