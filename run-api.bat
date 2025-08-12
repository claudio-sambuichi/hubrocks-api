@echo off
echo Starting HubRocks API...
echo.

REM Clean and build the project
echo Cleaning project...
dotnet clean > nul 2>&1

echo Building project...
dotnet build --configuration Debug --no-restore
if errorlevel 1 (
    echo Build failed!
    pause
    exit /b 1
)

echo.
echo Starting API server...
echo You can access:
echo - API: http://localhost:5000
echo - Swagger: http://localhost:5000/swagger
echo.
echo Press Ctrl+C to stop the server
echo.

REM Set environment to Development
set ASPNETCORE_ENVIRONMENT=Development

REM Run the DLL directly instead of the exe
dotnet "bin\Debug\net8.0\HubRocksApi.dll" --urls="http://localhost:5000" --environment=Development