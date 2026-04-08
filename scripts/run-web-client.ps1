Param()

$ErrorActionPreference = "Stop"

Write-Host "=== Smart Content Recommender: API + WebClient ===" -ForegroundColor Cyan

function Start-InNewWindow {
    Param(
        [string]$Title,
        [string]$Command
    )

    Start-Process powershell -ArgumentList @(
        "-NoExit",
        "-Command",
        $Command
    )
}

$repoRoot = Join-Path $PSScriptRoot ".."

Write-Host "Applying EF migrations..." -ForegroundColor Yellow
dotnet ef database update --project "$repoRoot\src\SmartContentRecommender.Infrastructure\SmartContentRecommender.Infrastructure.csproj" --startup-project "$repoRoot\src\SmartContentRecommender.WebAPI\SmartContentRecommender.WebAPI.csproj"

$existingApi = Get-NetTCPConnection -LocalPort 5078 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $existingApi) {
    Write-Host "Starting WebAPI..." -ForegroundColor Yellow
    Start-InNewWindow "SCR WebAPI" "cd '$repoRoot'; dotnet run --launch-profile http --project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj"
}
else {
    Write-Host "WebAPI already running on 5078. Reusing existing instance." -ForegroundColor DarkYellow
}

Start-Sleep -Seconds 2

Write-Host "Starting WebClient..." -ForegroundColor Yellow
Start-InNewWindow "SCR WebClient" "cd '$repoRoot'; dotnet run --launch-profile http --project src/SmartContentRecommender.WebClient/SmartContentRecommender.WebClient.csproj"

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "Swagger: http://localhost:5078/swagger"
Write-Host "WebClient: http://localhost:5133/"
Write-Host "Stop services with Ctrl+C in opened terminals."

