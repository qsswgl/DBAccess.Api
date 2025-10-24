@echo off
REM Production deployment script for HTTPS support

echo 🚀 Starting DBAccess.Api with HTTPS support...

REM Check if certificate exists
set CERT_PATH=.\certificates\qsgl.net.pfx
if not exist "%CERT_PATH%" (
    echo ❌ Certificate not found: %CERT_PATH%
    echo    Please place the qsgl.net.pfx certificate in the certificates directory
    exit /b 1
)

REM Check if certificate password is set
if "%CERT_PASSWORD%"=="" (
    echo ❌ Certificate password not set
    echo    Please set the CERT_PASSWORD environment variable
    echo    Example: set CERT_PASSWORD=your-password
    exit /b 1
)

REM Set production environment variables
set ASPNETCORE_ENVIRONMENT=Production
set ASPNETCORE_URLS=https://3950.qsgl.net:5190;http://3950.qsgl.net:5189

echo ✅ Certificate found: %CERT_PATH%
echo ✅ Environment: %ASPNETCORE_ENVIRONMENT%
echo ✅ URLs: %ASPNETCORE_URLS%
echo.

REM Start the application
dotnet run