# Docker 构建和推送脚本 - .NET 8 版本
# 用于推送到私有仓库 43.138.35.183

param(
    [string]$Registry = "43.138.35.183",
    [string]$Port = "5000",
    [string]$ImageName = "dbaccess-api",
    [string]$Tag = "net8-latest",
    [switch]$Push = $false,
    [switch]$Test = $false
)

$FullRegistry = "${Registry}:${Port}"
$FullImageName = "${FullRegistry}/${ImageName}:${Tag}"
$LocalImageName = "${ImageName}:${Tag}"

Write-Host "🐳 开始构建 .NET 8 Docker 镜像..." -ForegroundColor Cyan
Write-Host "   镜像名: $FullImageName" -ForegroundColor Gray
Write-Host ""

# 确保在正确的目录
$ProjectPath = "k:\DBAccess\DBAccess.Api"
if (!(Test-Path $ProjectPath)) {
    Write-Host "❌ 项目路径不存在: $ProjectPath" -ForegroundColor Red
    exit 1
}

Set-Location $ProjectPath

# 检查 Docker 是否运行
try {
    docker version | Out-Null
} catch {
    Write-Host "❌ Docker 未运行或未安装" -ForegroundColor Red
    exit 1
}

# 1. 构建镜像
Write-Host "🔨 构建 Docker 镜像..." -ForegroundColor Yellow
$buildStart = Get-Date

docker build `
    -f Dockerfile.net8 `
    -t $LocalImageName `
    -t $FullImageName `
    --build-arg BUILDKIT_INLINE_CACHE=1 `
    .

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Docker 构建失败" -ForegroundColor Red
    exit 1
}

$buildTime = ((Get-Date) - $buildStart).TotalSeconds
Write-Host "✅ 镜像构建成功 (耗时: $([math]::Round($buildTime, 1))秒)" -ForegroundColor Green

# 2. 显示镜像信息
Write-Host "" 
Write-Host "📊 镜像信息:" -ForegroundColor Cyan
docker images | Select-String $ImageName

# 3. 测试镜像（可选）
if ($Test) {
    Write-Host ""
    Write-Host "🧪 测试镜像功能..." -ForegroundColor Yellow
    
    # 启动测试容器
    $containerId = docker run -d `
        -p 18080:8080 `
        -e DBACCESS_MSSQL_SERVER=61.163.200.245 `
        -e DBACCESS_MSSQL_USER=sa `
        -e DBACCESS_MSSQL_PASSWORD=GalaxyS24 `
        --name "dbaccess-test" `
        $LocalImageName
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   测试容器启动: $containerId" -ForegroundColor Gray
        
        # 等待容器启动
        Write-Host "   等待应用启动..." -ForegroundColor Gray
        Start-Sleep -Seconds 10
        
        # 测试健康检查
        try {
            $response = Invoke-RestMethod -Uri "http://localhost:18080/api/dbaccess/ping?db=master" -TimeoutSec 30
            Write-Host "✅ 应用健康检查通过" -ForegroundColor Green
        } catch {
            Write-Host "⚠️  健康检查失败: $($_.Exception.Message)" -ForegroundColor Yellow
        }
        
        # 清理测试容器
        docker stop "dbaccess-test" | Out-Null
        docker rm "dbaccess-test" | Out-Null
        Write-Host "   清理测试容器完成" -ForegroundColor Gray
    } else {
        Write-Host "❌ 测试容器启动失败" -ForegroundColor Red
    }
}

# 4. 推送到私有仓库（可选）
if ($Push) {
    Write-Host ""
    Write-Host "📤 推送镜像到私有仓库..." -ForegroundColor Yellow
    Write-Host "   目标仓库: $FullRegistry" -ForegroundColor Gray
    
    # 检查私有仓库连接
    try {
        $testResult = Test-NetConnection -ComputerName $Registry -Port $Port -InformationLevel Quiet
        if (!$testResult.TcpTestSucceeded) {
            Write-Host "❌ 无法连接到私有仓库 ${Registry}:${Port}" -ForegroundColor Red
            Write-Host "   请检查仓库地址和网络连接" -ForegroundColor Gray
            exit 1
        }
    } catch {
        Write-Host "⚠️  网络测试异常: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # 配置 Docker 信任私有仓库（如果需要）
    Write-Host "   配置 Docker 信任私有仓库..." -ForegroundColor Gray
    
    # 推送镜像
    $pushStart = Get-Date
    docker push $FullImageName
    
    if ($LASTEXITCODE -eq 0) {
        $pushTime = ((Get-Date) - $pushStart).TotalSeconds
        Write-Host "✅ 镜像推送成功 (耗时: $([math]::Round($pushTime, 1))秒)" -ForegroundColor Green
        Write-Host "   镜像地址: $FullImageName" -ForegroundColor Gray
    } else {
        Write-Host "❌ 镜像推送失败" -ForegroundColor Red
        Write-Host "   请检查私有仓库配置和认证" -ForegroundColor Gray
        exit 1
    }
}

Write-Host ""
Write-Host "🎉 操作完成!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 使用说明:" -ForegroundColor Cyan
Write-Host "   本地运行: docker run -p 8080:8080 $LocalImageName" -ForegroundColor Gray
Write-Host "   从私有仓库拉取: docker pull $FullImageName" -ForegroundColor Gray
Write-Host "   生产部署: docker run -d -p 80:8080 -p 443:8443 $FullImageName" -ForegroundColor Gray
Write-Host ""