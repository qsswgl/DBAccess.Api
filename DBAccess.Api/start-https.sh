#!/bin/bash
# Production deployment script for HTTPS support

echo "🚀 Starting DBAccess.Api with HTTPS support..."

# Check if certificate exists
CERT_PATH="./certificates/qsgl.net.pfx"
if [ ! -f "$CERT_PATH" ]; then
    echo "❌ Certificate not found: $CERT_PATH"
    echo "   Please place the qsgl.net.pfx certificate in the certificates directory"
    exit 1
fi

# Check if certificate password is set
if [ -z "$CERT_PASSWORD" ]; then
    echo "❌ Certificate password not set"
    echo "   Please set the CERT_PASSWORD environment variable"
    exit 1
fi

# Set production environment variables
export ASPNETCORE_ENVIRONMENT=Production
export ASPNETCORE_URLS="https://3950.qsgl.net:5190;http://3950.qsgl.net:5189"

echo "✅ Certificate found: $CERT_PATH"
echo "✅ Environment: $ASPNETCORE_ENVIRONMENT"
echo "✅ URLs: $ASPNETCORE_URLS"
echo ""

# Start the application
dotnet DBAccess.Api.dll