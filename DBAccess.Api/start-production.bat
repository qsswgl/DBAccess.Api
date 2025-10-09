@echo off
echo Starting DBAccess API with HTTPS support...
echo.

REM Set environment variables
set CERT_PASSWORD=123456
set ASPNETCORE_ENVIRONMENT=Production

echo Certificate password: %CERT_PASSWORD%
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo.

REM Change to project directory
cd /d "k:\DBAccess\DBAccess.Api"

REM Start the application 
echo Starting application...
dotnet run --no-launch-profile

pause