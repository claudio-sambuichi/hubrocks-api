# HubRocks API Startup Script
Write-Host "Starting HubRocks API..." -ForegroundColor Green
Write-Host ""

# Clean and build the project
Write-Host "Cleaning project..." -ForegroundColor Yellow
dotnet clean | Out-Null

Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Debug --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host ""
Write-Host "Starting API server..." -ForegroundColor Green
Write-Host "You can access:" -ForegroundColor Cyan
Write-Host "- API: http://localhost:5000" -ForegroundColor Cyan
Write-Host "- Swagger: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Run the DLL directly instead of the exe
try {
    dotnet "bin\Debug\net8.0\HubRocksApi.dll" --urls="http://localhost:5000" --environment=Development
}
catch {
    Write-Host "Error starting the application: $($_.Exception.Message)" -ForegroundColor Red
    Read-Host "Press Enter to exit"
}