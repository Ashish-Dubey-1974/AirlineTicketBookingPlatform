# ============================================================
# SkyBooker — Day 1 Setup Script (PowerShell)
# Run this script ONCE to set up migrations and verify setup
# Usage: .\setup-day1.ps1
# ============================================================

Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║     SkyBooker — Day 1 Setup Script          ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Step 1 — Check dotnet version
Write-Host "► Checking .NET version..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host "  ✓ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Step 2 — Restore packages
Write-Host ""
Write-Host "► Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore SkyBooker.sln
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Restore failed! Check your internet connection." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Packages restored" -ForegroundColor Green

# Step 3 — Build solution
Write-Host ""
Write-Host "► Building solution..." -ForegroundColor Yellow
dotnet build SkyBooker.Auth.API/SkyBooker.Auth.API.csproj -c Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Build failed! Check the error above." -ForegroundColor Red
    exit 1
}
Write-Host "  ✓ Build succeeded" -ForegroundColor Green

# Step 4 — Check dotnet-ef tool
Write-Host ""
Write-Host "► Checking dotnet-ef tool..." -ForegroundColor Yellow
$efVersion = dotnet ef --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "  dotnet-ef not found. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    Write-Host "  ✓ dotnet-ef installed" -ForegroundColor Green
} else {
    Write-Host "  ✓ dotnet-ef: $efVersion" -ForegroundColor Green
}

# Step 5 — Create migration
Write-Host ""
Write-Host "► Creating EF Core migration..." -ForegroundColor Yellow
Set-Location SkyBooker.Auth.API
dotnet ef migrations add InitialCreate --output-dir Migrations
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Migration creation failed!" -ForegroundColor Red
    Write-Host "  Make sure SQL Server is running and connection string is correct." -ForegroundColor Yellow
    Set-Location ..
    exit 1
}
Write-Host "  ✓ Migration 'InitialCreate' created" -ForegroundColor Green

# Step 6 — Apply migration
Write-Host ""
Write-Host "► Applying migration to database..." -ForegroundColor Yellow
dotnet ef database update
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ✗ Database update failed!" -ForegroundColor Red
    Write-Host "  Check: Is SQL Server running? Is connection string correct?" -ForegroundColor Yellow
    Set-Location ..
    exit 1
}
Write-Host "  ✓ Database 'SkyBookerAuthDb' created with 'users' table" -ForegroundColor Green

Set-Location ..

# Done!
Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║        ✅  Day 1 Setup Complete!             ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Run: dotnet run --project SkyBooker.Auth.API" -ForegroundColor White
Write-Host "  2. Open: http://localhost:5001 (Swagger UI)" -ForegroundColor White
Write-Host "  3. Test: POST /api/auth/register with your details" -ForegroundColor White
Write-Host "  4. Test: POST /api/auth/login to get your JWT token" -ForegroundColor White
Write-Host ""
Write-Host "Day 2 task: Implement Airline.API + Complete Google OAuth" -ForegroundColor Yellow
