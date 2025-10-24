# 域名访问故障排除指南

## 🔍 问题分析
域名 `https://3950.qsgl.net:5190/` 无法访问，但 IP 地址 `https://192.168.137.101:5190/` 可以访问。

## ✅ 已确认的配置
1. DNS 解析正常：`3950.qsgl.net` -> `115.48.61.93`
2. 应用程序正确绑定到所有接口 (`0.0.0.0:5190`)
3. Windows 防火墙规则已创建
4. HTTPS 证书配置正确

## ❌ 可能的问题点

### 1. 网络地址转换 (NAT) 问题
**问题**: DNS 解析到的公网 IP (`115.48.61.93`) 与内网 IP (`192.168.137.101`) 不匹配
**解决方案**:
```powershell
# 检查本机公网 IP
Invoke-RestMethod -Uri "http://ipinfo.io/ip"

# 如果公网 IP 与 DNS 记录不一致，需要更新 DNS 记录
```

### 2. 路由器端口转发未配置
**问题**: 外部访问需要路由器将端口 5190 转发到内网机器
**解决方案**:
- 登录路由器管理界面
- 配置端口转发：外部端口 5190 -> 192.168.137.101:5190
- 或配置 DMZ 主机指向 192.168.137.101

### 3. ISP 端口限制
**问题**: 一些 ISP 可能阻止非标准端口
**解决方案**:
- 尝试使用标准 HTTPS 端口 443
- 或联系 ISP 确认端口 5190 是否被阻止

### 4. 域名解析缓存问题
**问题**: 本地 DNS 缓存可能导致解析问题
**解决方案**:
```powershell
# 清除 DNS 缓存
ipconfig /flushdns

# 使用不同 DNS 服务器测试
nslookup 3950.qsgl.net 8.8.8.8
```

## 🔧 推荐配置步骤

### 步骤 1: 确认网络环境
```powershell
# 1. 检查本机公网 IP
$publicIP = Invoke-RestMethod -Uri "http://ipinfo.io/ip"
Write-Host "本机公网 IP: $publicIP"

# 2. 检查 DNS 解析
$dnsResult = Resolve-DnsName -Name "3950.qsgl.net" -Type A
Write-Host "DNS 解析结果: $($dnsResult.IPAddress)"

# 3. 比较是否一致
if ($publicIP -eq $dnsResult.IPAddress) {
    Write-Host "✅ IP 地址匹配"
} else {
    Write-Host "❌ IP 地址不匹配，需要更新 DNS 记录或配置路由器"
}
```

### 步骤 2: 配置路由器端口转发
1. 访问路由器管理界面 (通常是 192.168.1.1 或 192.168.0.1)
2. 找到"端口转发"或"虚拟服务器"设置
3. 添加规则：
   - 服务名称: DBAccess API
   - 外部端口: 5190
   - 内部 IP: 192.168.137.101
   - 内部端口: 5190
   - 协议: TCP

### 步骤 3: 使用标准端口 (可选)
如果端口 5190 仍有问题，建议改用 443 端口：

1. 修改 `launchSettings.json`:
```json
"production": {
  "applicationUrl": "https://3950.qsgl.net:443;http://3950.qsgl.net:80"
}
```

2. 修改 `Program.cs`:
```csharp
options.ListenAnyIP(80);  // HTTP
options.ListenAnyIP(443, listenOptions => { /* HTTPS config */ });
```

3. 配置路由器转发端口 443 -> 192.168.137.101:443

## 🧪 测试命令

### 本机测试
```powershell
# 测试本地访问
curl -k https://localhost:5190/
curl -k https://127.0.0.1:5190/

# 测试内网 IP 访问
curl -k https://192.168.137.101:5190/
```

### 外网测试
```bash
# 在其他网络环境中测试
curl -k https://3950.qsgl.net:5190/

# 测试端口连通性
telnet 3950.qsgl.net 5190
```

## 📞 快速解决方案

### 临时解决方案 1: 使用内网 IP
如果只需要内网访问，直接使用 `https://192.168.137.101:5190/`

### 临时解决方案 2: 修改 hosts 文件
在客户端机器的 hosts 文件中添加：
```
192.168.137.101  3950.qsgl.net
```

### 永久解决方案: 配置网络基础设施
1. 确保 DNS 记录指向正确的公网 IP
2. 配置路由器端口转发
3. 可能需要联系网络管理员或 ISP

## 📋 检查清单
- [ ] 本机公网 IP 与 DNS 记录一致
- [ ] 路由器端口转发已配置
- [ ] Windows 防火墙规则已创建
- [ ] 应用程序正在监听 0.0.0.0:5190
- [ ] 从外网可以 telnet 到端口 5190
- [ ] 证书包含正确的域名 (3950.qsgl.net)