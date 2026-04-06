Param(
    [switch]$Microservices
)

$ErrorActionPreference = "Stop"

Write-Host "=== Smart Content Recommender demo launcher ===" -ForegroundColor Cyan

function Start-InNewWindow {
    Param(
        [string]$Title,
        [string]$Command
    )

    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        "$host.UI.RawUI.WindowTitle = '$Title'; $Command"
    )
}

if ($Microservices) {
    Write-Host "Starting microservice demo profile..." -ForegroundColor Yellow
    Start-InNewWindow "SCR AuthService" "cd '$PSScriptRoot\..\'; dotnet run --project src/SmartContentRecommender.AuthService/SmartContentRecommender.AuthService.csproj"
    Start-InNewWindow "SCR ContentService" "cd '$PSScriptRoot\..\'; dotnet run --project src/SmartContentRecommender.ContentService/SmartContentRecommender.ContentService.csproj"
    Start-InNewWindow "SCR RecommendationService" "cd '$PSScriptRoot\..\'; dotnet run --project src/SmartContentRecommender.RecommendationService/SmartContentRecommender.RecommendationService.csproj"
}
else {
    Write-Host "Starting monolith API profile..." -ForegroundColor Yellow
    Start-InNewWindow "SCR WebAPI" "cd '$PSScriptRoot\..\'; dotnet run --project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj"
}

Start-Sleep -Seconds 2

Write-Host "Starting WPF client..." -ForegroundColor Yellow
Start-InNewWindow "SCR WpfClient" "cd '$PSScriptRoot\..\'; dotnet run --project src/SmartContentRecommender.WpfClient/SmartContentRecommender.WpfClient.csproj"

Write-Host ""
Write-Host "Done. Tips:" -ForegroundColor Green
Write-Host "1) Swagger (monolith): http://localhost:5078/swagger"
Write-Host "2) Use demo users from README."
Write-Host "3) Stop any service with Ctrl+C in its terminal."

