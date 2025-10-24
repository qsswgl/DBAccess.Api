Write-Host "Network Configuration Check for Domain Access"
Write-Host "=============================================="

# 1. Check Public IP
Write-Host "1. Checking Public IP..."
try {
    $publicIP = Invoke-RestMethod -Uri "http://ipinfo.io/ip" -TimeoutSec 10
    Write-Host "   Public IP: $publicIP"
} catch {
    Write-Host "   Failed to get public IP: $($_.Exception.Message)"
    $publicIP = "Unknown"
}

# 2. Check DNS Resolution
Write-Host "2. Checking DNS Resolution..."
try {
    $dnsResult = Resolve-DnsName -Name "3950.qsgl.net" -Type A -ErrorAction Stop
    $dnsIP = $dnsResult[0].IPAddress
    Write-Host "   DNS IP: $dnsIP"
} catch {
    Write-Host "   DNS resolution failed: $($_.Exception.Message)"
    $dnsIP = "Unknown"
}

# 3. Compare IPs
Write-Host "3. IP Comparison..."
if ($publicIP -ne "Unknown" -and $dnsIP -ne "Unknown") {
    if ($publicIP -eq $dnsIP) {
        Write-Host "   Status: MATCH - IPs are identical"
    } else {
        Write-Host "   Status: MISMATCH - Router port forwarding needed"
        Write-Host "   Action: Configure router to forward port 5190 to 192.168.137.101"
    }
} else {
    Write-Host "   Status: CANNOT_COMPARE - Network issues detected"
}

# 4. Check if application is running
Write-Host "4. Checking Application Status..."
$netstat5190 = netstat -ano | findstr ":5190"
$netstat5189 = netstat -ano | findstr ":5189"

if ($netstat5190) {
    Write-Host "   Port 5190 (HTTPS): LISTENING"
} else {
    Write-Host "   Port 5190 (HTTPS): NOT LISTENING"
}

if ($netstat5189) {
    Write-Host "   Port 5189 (HTTP): LISTENING"  
} else {
    Write-Host "   Port 5189 (HTTP): NOT LISTENING"
}

# 5. Recommendations
Write-Host ""
Write-Host "RECOMMENDATIONS:"
Write-Host "=================="

if ($publicIP -eq $dnsIP) {
    Write-Host "✅ DNS and Public IP match - should work from external networks"
    Write-Host "   If still not working, check ISP port blocking"
} else {
    Write-Host "⚠️  DNS and Public IP don't match"
    Write-Host "   Option 1: Update DNS record to point to $publicIP"
    Write-Host "   Option 2: Configure router port forwarding:"
    Write-Host "            External: 5190 -> Internal: 192.168.137.101:5190"
    Write-Host "   Option 3: Use IP address directly: https://$publicIP`:5190/"
}

if (-not $netstat5190) {
    Write-Host "❌ Application not running - start with:"
    Write-Host '   $env:CERT_PASSWORD="testpassword123"'
    Write-Host '   dotnet run --project "K:\DBAccess\DBAccess.Api\DBAccess.Api.csproj" --launch-profile https'
}

Write-Host ""
Write-Host "QUICK TESTS:"
Write-Host "============="
Write-Host "Local access:  https://localhost:5190/"
Write-Host "LAN access:    https://192.168.137.101:5190/"  
Write-Host "Domain access: https://3950.qsgl.net:5190/"