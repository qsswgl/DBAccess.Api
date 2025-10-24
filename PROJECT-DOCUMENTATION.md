# DBAccess.Api 项目文档

## 📋 项目概述

DBAccess.Api 是一个基于 .NET 8 的高性能 Web API 服务，用于通过 HTTP/HTTPS 接口调用 SQL Server 存储过程。

### 核心特性

- ✅ **.NET 8 AOT 编译** - 原生镜像支持，启动快速，内存占用低
- ✅ **完整的 Swagger 文档** - 可视化 API 测试界面
- ✅ **Docker 容器化部署** - 支持多平台部署
- ✅ **HTTPS 安全通信** - 使用 SSL/TLS 加密
- ✅ **健康检查机制** - 自动监控服务状态
- ✅ **动态存储过程调用** - 支持任意存储过程执行

---

## 🌐 生产环境信息

### 服务器信息
- **主机名**: tx.qsgl.net
- **服务器IP**: 腾讯云服务器
- **操作系统**: Ubuntu Linux
- **Docker版本**: 最新稳定版

### 容器信息
- **容器名称**: dbaccess-api
- **镜像**: 43.138.35.183:5000/dbaccess-api:net8-healthy-fix
- **运行时间**: 持续运行（unless-stopped）
- **资源限制**: 默认配置

---

## 🔗 接口地址

### 基础 URL

| 协议 | 端口 | 地址 | 用途 |
|------|------|------|------|
| HTTP | 80 | http://tx.qsgl.net | 标准HTTP访问 |
| HTTP | 5189 | http://tx.qsgl.net:5189 | 备用HTTP端口 |
| HTTPS | 5190 | https://tx.qsgl.net:5190 | **推荐使用** - 安全HTTPS访问 |

### Swagger 文档地址

```
https://tx.qsgl.net:5190/swagger/index.html
```

### API 端点

#### 1. 健康检查
```http
GET /api/dbaccess/ping?db={数据库名}
```

**示例**:
```bash
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"
```

**响应**:
```json
{
  "status": "healthy",
  "database": "master",
  "timestamp": "2025-10-24T08:30:00Z"
}
```

#### 2. 调用存储过程
```http
POST /Qsoft/procedure/{存储过程名}
```

**请求头**:
```
Content-Type: application/json
Accept: application/json
```

**请求体**:
```json
{
  "Client_IP": "客户端IP地址",
  "domain": "域名",
  "sub_domain": "子域名"
}
```

**示例 - DNSPOD_UPDATE 存储过程**:
```bash
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -d '{
    "Client_IP": "115.48.63.233",
    "domain": "qsgl",
    "sub_domain": "3950"
  }'
```

**成功响应**:
```json
{
  "code": 0,
  "message": "执行成功",
  "data": "解析成功,域名3950.qsgl已解析到:115.48.63.233"
}
```

**失败响应**:
```json
{
  "code": -1,
  "message": "执行失败",
  "error": "错误详细信息"
}
```

---

## 🧪 生产环境测试

### 测试准备

1. **工具准备**
   - curl 命令行工具
   - Postman 或类似 API 测试工具
   - 浏览器（用于访问 Swagger）

2. **网络要求**
   - 能够访问 tx.qsgl.net
   - 端口 5190 (HTTPS) 或 5189 (HTTP) 开放

### 测试用例

#### 测试 1: Swagger 界面访问
```bash
# 浏览器访问
https://tx.qsgl.net:5190/swagger/index.html

# 预期结果: 显示完整的 Swagger UI 界面
```

#### 测试 2: 健康检查
```bash
# HTTP 测试
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"

# HTTPS 测试（推荐）
curl -k "https://tx.qsgl.net:5190/api/dbaccess/ping?db=master"

# 预期结果: 返回 200 OK 和健康状态信息
```

#### 测试 3: DNSPOD_UPDATE 存储过程
```bash
# 使用 curl 测试
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "115.48.63.233",
    "domain": "qsgl",
    "sub_domain": "test"
  }'

# 预期结果: 返回存储过程执行结果
```

#### 测试 4: PowerShell 测试（Windows）
```powershell
# 准备请求体
$body = @{
    Client_IP = "115.48.63.233"
    domain = "qsgl"
    sub_domain = "test"
} | ConvertTo-Json

# 发送请求
Invoke-RestMethod -Uri "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body `
    -SkipCertificateCheck

# 注意: PowerShell 5.1 不支持 -SkipCertificateCheck，需要 PowerShell 7+
```

#### 测试 5: 性能测试
```bash
# 使用 Apache Bench (ab) 进行压力测试
ab -n 1000 -c 10 "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"

# 参数说明:
# -n 1000: 总共发送 1000 个请求
# -c 10: 并发 10 个请求
```

### 测试检查清单

- [ ] Swagger UI 正常显示
- [ ] HTTP 端口 (80, 5189) 可访问
- [ ] HTTPS 端口 (5190) 可访问且证书有效
- [ ] 健康检查返回 200 状态码
- [ ] 存储过程调用返回正确结果
- [ ] JSON 响应格式正确
- [ ] 错误处理机制工作正常
- [ ] 并发请求处理正常

---

## 🗄️ 数据库配置

### 连接信息
- **服务器**: 61.163.200.245
- **用户名**: sa
- **密码**: GalaxyS24（通过环境变量配置）
- **默认数据库**: 根据存储过程需求动态切换

### 支持的存储过程

当前已测试的存储过程：

1. **DNSPOD_UPDATE** - DNS 解析更新
   - 参数: Client_IP, domain, sub_domain
   - 用途: 动态更新 DNS 解析记录

*其他存储过程可通过相同方式调用*

---

## 🔒 安全配置

### SSL/TLS 证书
- **证书文件**: qsgl.net.pfx
- **证书路径**: /opt/shared-certs/qsgl.net.pfx
- **证书密码**: qsgl2024（环境变量）
- **证书类型**: 通配符证书 (*.qsgl.net)

### 环境变量
```bash
DBACCESS_MSSQL_SERVER=61.163.200.245
DBACCESS_MSSQL_USER=sa
DBACCESS_MSSQL_PASSWORD=GalaxyS24
CERT_PASSWORD=qsgl2024
```

### 防火墙规则
```bash
# 允许的端口
80/tcp    - HTTP
5189/tcp  - HTTP (备用)
5190/tcp  - HTTPS (主要)
```

---

## 🐳 Docker 部署

### 当前运行容器

```bash
docker ps --filter name=dbaccess-api
```

**输出示例**:
```
CONTAINER ID   IMAGE                                                 STATUS              PORTS
869f0938b052   43.138.35.183:5000/dbaccess-api:net8-healthy-fix     Up 1 hour           0.0.0.0:80->8080/tcp, 
                                                                                         0.0.0.0:5189->8080/tcp,
                                                                                         0.0.0.0:5190->8443/tcp
```

### 启动命令

```bash
docker run -d \
  --name dbaccess-api \
  --restart unless-stopped \
  -p 80:8080 \
  -p 5189:8080 \
  -p 5190:8443 \
  -v /opt/shared-certs:/app/certificates:ro \
  -e DBACCESS_MSSQL_SERVER=61.163.200.245 \
  -e DBACCESS_MSSQL_USER=sa \
  -e DBACCESS_MSSQL_PASSWORD=GalaxyS24 \
  -e CERT_PASSWORD=qsgl2024 \
  --health-cmd='curl -f http://localhost:8080/api/dbaccess/ping?db=master || exit 1' \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  43.138.35.183:5000/dbaccess-api:net8-healthy-fix
```

### 容器管理命令

```bash
# 查看日志
docker logs -f dbaccess-api

# 查看最近 100 行日志
docker logs --tail 100 dbaccess-api

# 重启容器
docker restart dbaccess-api

# 停止容器
docker stop dbaccess-api

# 删除容器
docker rm -f dbaccess-api

# 进入容器
docker exec -it dbaccess-api bash
```

---

## 📊 监控和维护

### 健康检查

容器配置了自动健康检查：
- **检查间隔**: 30秒
- **超时时间**: 10秒
- **重试次数**: 3次
- **检查命令**: `curl -f http://localhost:8080/api/dbaccess/ping?db=master`

查看健康状态：
```bash
docker inspect dbaccess-api --format='{{.State.Health.Status}}'
```

### 日志管理

```bash
# 实时查看日志
ssh root@tx.qsgl.net "docker logs -f dbaccess-api"

# 查询错误日志
ssh root@tx.qsgl.net "docker logs dbaccess-api 2>&1 | grep -i error"

# 查询最近的请求
ssh root@tx.qsgl.net "docker logs --since 1h dbaccess-api"
```

### 性能指标

```bash
# 查看容器资源使用
docker stats dbaccess-api --no-stream

# 预期输出
CONTAINER ID   NAME           CPU %   MEM USAGE / LIMIT   NET I/O
869f0938b052   dbaccess-api   0.5%    50MB / 16GB         1.2MB / 800KB
```

---

## 🔄 更新和部署流程

### 1. 代码更新

```bash
# 在开发机器上
cd K:\DBAccess
git pull origin main
git add .
git commit -m "更新说明"
git push origin main
```

### 2. 服务器拉取代码

```bash
ssh root@tx.qsgl.net
cd /root/DBAccess.Api
git pull
```

### 3. 构建新镜像

```bash
cd /root/DBAccess.Api/DBAccess.Api
docker build -f Dockerfile.net8 -t 43.138.35.183:5000/dbaccess-api:latest .
```

### 4. 停止旧容器

```bash
docker stop dbaccess-api
docker rm dbaccess-api
```

### 5. 启动新容器

```bash
# 使用上面的启动命令，将镜像标签改为 latest
docker run -d --name dbaccess-api ... 43.138.35.183:5000/dbaccess-api:latest
```

### 6. 验证部署

```bash
# 检查容器状态
docker ps | grep dbaccess-api

# 检查日志
docker logs --tail 50 dbaccess-api

# 测试 API
curl -k "https://tx.qsgl.net:5190/api/dbaccess/ping?db=master"
```

---

## 🛠️ 故障排查

### 问题 1: 容器无法启动

**症状**: `docker ps` 看不到容器

**解决方案**:
```bash
# 查看所有容器（包括停止的）
docker ps -a | grep dbaccess-api

# 查看启动日志
docker logs dbaccess-api

# 常见原因：
# - 端口被占用
# - 环境变量配置错误
# - 证书文件不存在
```

### 问题 2: HTTPS 不可用

**症状**: HTTPS 端口无响应

**解决方案**:
```bash
# 检查证书文件
ls -la /opt/shared-certs/qsgl.net.pfx

# 检查容器日志
docker logs dbaccess-api | grep -i https

# 验证证书挂载
docker inspect dbaccess-api | grep -A 5 Mounts
```

### 问题 3: 数据库连接失败

**症状**: API 返回数据库错误

**解决方案**:
```bash
# 检查数据库连接
telnet 61.163.200.245 1433

# 检查环境变量
docker exec dbaccess-api printenv | grep DBACCESS

# 验证数据库凭据
docker exec dbaccess-api /opt/mssql-tools/bin/sqlcmd \
  -S 61.163.200.245 -U sa -P GalaxyS24 -Q "SELECT @@VERSION"
```

### 问题 4: 健康检查失败

**症状**: 容器状态显示 unhealthy

**解决方案**:
```bash
# 手动执行健康检查
docker exec dbaccess-api curl -f http://localhost:8080/api/dbaccess/ping?db=master

# 查看健康检查历史
docker inspect dbaccess-api --format='{{json .State.Health}}' | jq

# 暂时禁用健康检查（重新创建容器时去掉 --health-* 参数）
```

---

## 📝 开发者指南

### 本地开发环境

```bash
# 克隆仓库
git clone https://github.com/qsswgl/DBAccess.Api.git
cd DBAccess.Api/DBAccess.Api

# 恢复依赖
dotnet restore

# 运行开发服务器
dotnet run

# 访问本地 Swagger
http://localhost:5000/swagger
```

### 添加新的存储过程支持

1. **无需修改代码** - API 支持动态调用任何存储过程

2. **只需准备参数** - 按照存储过程的参数要求构造 JSON

3. **示例**:
```bash
# 调用名为 MY_PROCEDURE 的存储过程
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/MY_PROCEDURE" \
  -H "Content-Type: application/json" \
  -d '{
    "param1": "value1",
    "param2": "value2"
  }'
```

### 代码结构

```
DBAccess.Api/
├── Controllers/
│   └── DbAccessController.cs    # 主控制器
├── Models/
│   └── ProcedureInputModel.cs   # 输入模型
├── Json/
│   └── AppJsonContext.cs        # AOT JSON 序列化上下文
├── Program.cs                   # 应用启动配置
├── appsettings.json            # 配置文件
├── Dockerfile.net8             # Docker 构建文件
└── DBAccess.Api.csproj         # 项目文件
```

---

## 📞 技术支持

### GitHub 仓库
- **地址**: https://github.com/qsswgl/DBAccess.Api
- **分支**: main
- **访问方式**: SSH over HTTPS (端口 443)

### 联系方式
- **维护者**: qsswgl
- **更新日期**: 2025年10月24日

---

## 📋 变更日志

### 2025-10-24
- ✅ 修复 AppJsonContext - 添加 ValidationProblemDetails 支持
- ✅ 生成 PFX 证书文件 (qsgl.net.pfx)
- ✅ HTTPS 成功启用
- ✅ 部署 net8-healthy-fix 镜像
- ✅ 完善项目文档

### 2025-10-16
- ✅ 初始项目创建
- ✅ .NET 8 AOT 编译支持
- ✅ Swagger 文档集成
- ✅ Docker 容器化部署

---

## 📄 许可证

本项目采用私有许可证，仅供内部使用。

---

**最后更新**: 2025年10月24日  
**文档版本**: 1.0
