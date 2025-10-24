# DBAccess.Api

🚀 基于 .NET 8 的高性能 SQL Server 存储过程调用 Web API

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Docker](https://img.shields.io/badge/Docker-Enabled-blue)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-Private-red)](LICENSE)

---

## 📋 项目简介

DBAccess.Api 是一个轻量级、高性能的 Web API 服务，提供通过 HTTP/HTTPS 接口调用 SQL Server 存储过程的能力。基于 .NET 8 AOT 编译，支持 Docker 容器化部署，具有完整的 Swagger 文档和 HTTPS 安全通信。

### ✨ 核心特性

- 🚀 **高性能** - .NET 8 AOT 原生镜像编译，启动快速，内存占用低
- 📚 **完整文档** - 集成 Swagger UI，可视化 API 测试
- 🐳 **容器化** - Docker 支持，一键部署
- 🔒 **安全通信** - HTTPS/TLS 加密传输
- 💪 **健壮性** - 健康检查、错误处理、日志记录
- 🔧 **灵活调用** - 支持动态调用任意存储过程

---

## 🌐 在线访问

### 生产环境

- **Swagger 文档**: https://tx.qsgl.net:5190/swagger/index.html
- **API 基础 URL**: https://tx.qsgl.net:5190
- **HTTP 备用端口**: http://tx.qsgl.net:5189

### 快速测试

```bash
# 健康检查
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"

# 调用存储过程
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{"Client_IP":"115.48.63.233","domain":"qsgl","sub_domain":"test"}'
```

---

## 📖 文档导航

- 📘 [**项目完整文档**](PROJECT-DOCUMENTATION.md) - 详细的项目说明、API文档、部署指南
- 🧪 [**生产环境测试指南**](PRODUCTION-TEST-GUIDE.md) - 完整的测试用例、自动化脚本、测试报告模板
- 🔧 [**故障排查文档**](DBAccess.Api/DOMAIN-ACCESS-TROUBLESHOOTING.md) - 常见问题和解决方案

---

## 🚀 快速开始

### 前置要求

- .NET 8 SDK
- Docker (可选，用于容器化部署)
- SQL Server 数据库访问权限

### 本地运行

```bash
# 克隆仓库
git clone https://github.com/qsswgl/DBAccess.Api.git
cd DBAccess.Api/DBAccess.Api

# 配置数据库连接（修改 appsettings.json）
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=master;User Id=sa;Password=YOUR_PASSWORD;"
  }
}

# 运行项目
dotnet run

# 访问 Swagger
浏览器打开: https://localhost:5001/swagger
```

### Docker 部署

```bash
# 构建镜像
docker build -f DBAccess.Api/Dockerfile.net8 -t dbaccess-api:latest .

# 运行容器
docker run -d \
  --name dbaccess-api \
  -p 8080:8080 \
  -p 8443:8443 \
  -e DBACCESS_MSSQL_SERVER=your_server \
  -e DBACCESS_MSSQL_USER=sa \
  -e DBACCESS_MSSQL_PASSWORD=your_password \
  dbaccess-api:latest
```

---

## 📡 API 端点

### 健康检查

```http
GET /api/dbaccess/ping?db={数据库名}
```

**响应示例**:
```json
{
  "status": "healthy",
  "database": "master",
  "timestamp": "2025-10-24T08:30:00Z"
}
```

### 调用存储过程

```http
POST /Qsoft/procedure/{存储过程名}
Content-Type: application/json

{
  "Client_IP": "字符串",
  "domain": "字符串",
  "sub_domain": "字符串"
}
```

**响应示例**:
```json
{
  "code": 0,
  "message": "执行成功",
  "data": "业务数据"
}
```

---

## 🔧 配置说明

### 环境变量

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `DBACCESS_MSSQL_SERVER` | SQL Server 地址 | `61.163.200.245` |
| `DBACCESS_MSSQL_USER` | 数据库用户名 | `sa` |
| `DBACCESS_MSSQL_PASSWORD` | 数据库密码 | `YourPassword` |
| `CERT_PASSWORD` | SSL 证书密码 | `qsgl2024` |

### 监控与邮件告警（环境变量）

| 变量名 | 说明 | 示例 |
|--------|------|------|
| `MONITOR_ENABLED` | 是否启用内置健康监控 | `true` |
| `MONITOR_HEALTH_URL` | 健康检查URL | `http://localhost:8080/api/dbaccess/ping?db=master` |
| `MONITOR_INTERVAL_SEC` | 检查间隔（秒） | `30` |
| `MONITOR_FAIL_THRESHOLD` | 连续失败阈值（触发告警） | `3` |
| `MONITOR_RECOVER_THRESHOLD` | 连续成功阈值（触发恢复通知） | `2` |
| `SMTP_HOST` | SMTP服务器 | `smtp.139.com` |
| `SMTP_PORT` | SMTP端口 | `465` |
| `SMTP_SSL` | 启用SSL/STARTTLS | `true` |
| `SMTP_USERNAME` | 发件人账号 | `qsoft@139.com` |
| `SMTP_PASSWORD` | 授权码（勿明文提交仓库） | `574a283d502db51ea200` |
| `SMTP_FROM` | 发件邮箱 | `qsoft@139.com` |
| `ALERT_EMAIL_TO` | 告警接收邮箱 | `qsoft@139.com` |

### 端口配置

| 端口 | 协议 | 用途 |
|------|------|------|
| 80 | HTTP | 标准HTTP访问 |
| 5189 | HTTP | 备用HTTP端口 |
| 5190 | HTTPS | 推荐使用的HTTPS端口 |

---

## 🏗️ 项目结构

```
DBAccess.Api/
├── Controllers/
│   └── DbAccessController.cs       # 主控制器
├── Models/
│   └── ProcedureInputModel.cs      # 请求模型
├── Json/
│   └── AppJsonContext.cs           # AOT JSON 序列化
├── Program.cs                      # 应用入口
├── appsettings.json               # 配置文件
├── Dockerfile.net8                # Docker 构建
└── DBAccess.Api.csproj            # 项目文件
```

---

## 🧪 测试

### 自动化测试脚本

```bash
# Bash (Linux/Mac)
./production-test-suite.sh

# PowerShell (Windows)
.\production-test-suite.ps1
```

### 手动测试

详见 [生产环境测试指南](PRODUCTION-TEST-GUIDE.md)

---

## 🐳 Docker Hub

当前版本镜像：

```bash
# 拉取镜像
docker pull 43.138.35.183:5000/dbaccess-api:net8-healthy-fix

# 运行容器
docker run -d -p 8080:8080 43.138.35.183:5000/dbaccess-api:net8-healthy-fix
```

---

## 📊 性能指标

- **启动时间**: < 2s
- **内存占用**: ~ 50MB
- **响应时间**: < 500ms (平均)
- **并发支持**: 100+ req/s

---

## 🔒 安全性

- ✅ HTTPS/TLS 1.2+ 加密通信
- ✅ SQL 参数化查询，防止注入
- ✅ 请求大小限制
- ✅ 错误信息脱敏
- ✅ 健康检查机制

---

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

### 开发流程

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

---

## 📝 更新日志

### [1.0.0] - 2025-10-24

#### 新增
- ✅ .NET 8 AOT 编译支持
- ✅ 完整 Swagger 文档
- ✅ Docker 容器化部署
- ✅ HTTPS 安全通信
- ✅ 健康检查端点
- ✅ 动态存储过程调用

#### 修复
- ✅ ValidationProblemDetails JSON 序列化
- ✅ SSL 证书配置
- ✅ 容器权限问题

---

## 📞 联系方式

- **GitHub**: [@qsswgl](https://github.com/qsswgl)
- **Issues**: [GitHub Issues](https://github.com/qsswgl/DBAccess.Api/issues)
- **Email**: [待补充]

---

## 📄 许可证

本项目采用私有许可证，仅供内部使用。

---

## 🙏 致谢

感谢以下开源项目：

- [ASP.NET Core](https://github.com/dotnet/aspnetcore)
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [Dapper](https://github.com/DapperLib/Dapper)

---

<div align="center">

**Made with ❤️ by qsswgl**

⭐ 如果这个项目对你有帮助，请给个星标！

</div>
