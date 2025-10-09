# DBAccess API HTTPS 部署指南

## 🎯 概述
DBAccess.Api 现已支持 HTTPS 连接，适用于生产环境部署在 `https://3950.qsgl.net:5190`。

## 📋 当前配置

### 端口配置
- **HTTP**: 5189 端口（开发和备用）
- **HTTPS**: 5190 端口（主要生产端口）

### 证书配置
- 证书路径: `certificates/qsgl.net.pfx`
- 环境变量: `CERT_PASSWORD`（证书密码）

## 🚀 部署步骤

### 1. 准备证书
将生产环境的 `qsgl.net.pfx` 证书文件放置在 `certificates/` 目录中。

### 2. 设置环境变量
```powershell
# Windows PowerShell
$env:CERT_PASSWORD="your-actual-certificate-password"

# Windows Command Prompt
set CERT_PASSWORD=your-actual-certificate-password

# Linux/Mac
export CERT_PASSWORD="your-actual-certificate-password"
```

### 3. 运行方式

#### 本地开发（HTTP）
```powershell
dotnet run --launch-profile http
```
访问: http://localhost:5189

#### 本地开发（HTTPS）
```powershell
$env:CERT_PASSWORD="your-password"
dotnet run --launch-profile https
```
访问: https://localhost:5190

#### 生产环境
```powershell
$env:CERT_PASSWORD="your-password"
dotnet run --launch-profile production
```
访问: https://3950.qsgl.net:5190

### 4. 使用启动脚本

#### Windows
```powershell
.\start-https.bat
```

#### Linux
```bash
chmod +x start-https.sh
./start-https.sh
```

## 🔧 配置文件说明

### Program.cs 配置
- 自动检测证书文件和密码
- 无证书时自动降级为 HTTP 模式
- 生产环境错误处理和日志记录

### launchSettings.json 配置文件
- `http`: 仅 HTTP 模式 (5189)
- `https`: HTTP + HTTPS 模式 (5189 + 5190)
- `production`: 生产环境配置 (3950.qsgl.net)

## 🐳 Docker 部署

### 构建镜像
```bash
docker build -t dbaccess-api:latest .
```

### 运行容器（带证书）
```bash
docker run -d \
  --name dbaccess-api \
  -p 8080:8080 \
  -p 8443:8443 \
  -v /path/to/certificates:/app/certificates:ro \
  -e CERT_PASSWORD="your-password" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  dbaccess-api:latest
```

## ✅ 验证部署

### 检查端点
- HTTP: `curl http://localhost:5189/`
- HTTPS: `curl -k https://localhost:5190/`

### 检查日志
启动时应看到以下消息：
```
✅ HTTPS enabled with certificate: certificates/qsgl.net.pfx
info: Now listening on: http://[::]:5189
info: Now listening on: https://[::]:5190
```

## 🛡️ 安全注意事项

1. **证书管理**
   - 保护证书文件权限 (600 或更严格)
   - 定期更新证书
   - 安全存储证书密码

2. **环境变量**
   - 生产环境中通过安全方式设置 `CERT_PASSWORD`
   - 避免在脚本中硬编码密码

3. **网络配置**
   - 确保防火墙允许 5190 端口
   - 配置反向代理（如 Nginx）进行负载均衡

## 🔍 故障排除

### 常见问题

#### 证书加载失败
```
❌ Failed to load certificate: 指定的网络密码不正确
```
**解决方法**: 检查 `CERT_PASSWORD` 环境变量

#### 证书未找到
```
⚠️  Certificate not found: certificates/qsgl.net.pfx
```
**解决方法**: 确保证书文件在正确位置

#### 端口占用
```
Failed to bind to address http://[::]:5189: address already in use
```
**解决方法**: 停止其他进程或更改端口

### 调试命令
```powershell
# 检查端口占用
netstat -ano | findstr 5189
netstat -ano | findstr 5190

# 检查证书文件
Test-Path "certificates/qsgl.net.pfx"

# 检查环境变量
echo $env:CERT_PASSWORD
```

## 📞 支持信息
如有问题，请检查应用程序日志和上述故障排除指南。