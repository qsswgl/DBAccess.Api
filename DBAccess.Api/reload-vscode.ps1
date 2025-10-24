# VS Code 配置强制重载脚本
Write-Host "===  强制重载VS Code配置 ===" -ForegroundColor Cyan

# 1. 关闭所有VS Code进程
Write-Host "1. 正在关闭VS Code进程..." -ForegroundColor Yellow
Get-Process -Name "Code" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# 2. 清除VS Code缓存
Write-Host "2. 清除扩展缓存..." -ForegroundColor Yellow
$vscodeCache = "$env:APPDATA\Code\CachedExtensions"
if (Test-Path $vscodeCache) {
    Remove-Item $vscodeCache -Recurse -Force -ErrorAction SilentlyContinue
}

# 3. 验证配置文件
Write-Host "3. 验证配置文件..." -ForegroundColor Yellow
$settings = Get-Content "$env:APPDATA\Code\User\settings.json" -Raw
try {
    $json = ConvertFrom-Json $settings
    Write-Host "    配置文件语法正确" -ForegroundColor Green
} catch {
    Write-Host "    配置文件有错误: $($_.Exception.Message)" -ForegroundColor Red
}

# 4. 重新启动VS Code
Write-Host "4. 重新启动VS Code..." -ForegroundColor Yellow
Start-Process "code" -ArgumentList "."
Write-Host "    VS Code已重新启动" -ForegroundColor Green
Write-Host ""
Write-Host " 请在重启后的VS Code中测试自动审批功能" -ForegroundColor Cyan
