# SSL 证书配置

## 证书文件
请将 `qsgl.net.pfx` 证书文件放置在此目录中。

## 证书密码
设置环境变量 `CERT_PASSWORD` 来指定证书密码：
```powershell
$env:CERT_PASSWORD="your-certificate-password"
```

## 生产部署
确保在生产环境中正确设置以下环境变量：
- `CERT_PASSWORD`: 证书密码
- `ASPNETCORE_URLS`: 监听地址（可选，默认为 https://0.0.0.0:5190;http://0.0.0.0:5189）

## 本地开发
对于本地开发，如果没有证书文件，应用程序会在 HTTP 模式下运行在 5189 端口。