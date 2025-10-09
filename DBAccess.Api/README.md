# DBAccess.Api Microservice

一个基于 .NET 10.0 的 RESTful 数据访问微服务，支持存储过程、函数和表查询，可在 Ubuntu Docker 容器中独立部署。

## 🚀 快速开始

### 使用 Docker Compose（推荐）

1. **克隆或复制项目文件**
2. **配置环境变量**：
   ```bash
   cp .env.example .env
   # 编辑 .env 文件配置数据库连接
   ```
3. **构建并启动**：
   ```bash
   chmod +x build.sh deploy.sh
   ./build.sh
   ./deploy.sh
   ```

### 手动 Docker 部署

```bash
# 构建镜像
docker build -t dbaccess-api:latest .

# 运行容器
docker run -d \
  --name dbaccess-api \
  -p 8080:8080 \
  -e DBACCESS_MSSQL_SERVER=your-server \
  -e DBACCESS_MSSQL_USER=sa \
  -e DBACCESS_MSSQL_PASSWORD=your-password \
  dbaccess-api:latest
```

## 📋 API 端点

服务启动后，可通过以下端点访问：

### REST API
- `POST /{dbName}/procedure/{procedureName}` - 调用存储过程
- `GET /{dbName}/table/{tableName}` - 查询表数据
- `POST /{dbName}/function/{functionName}` - 调用表值函数

### 系统端点
- `GET /api/dbaccess/ping?db={dbName}` - 健康检查

### 示例调用

```bash
# 调用存储过程
curl -X POST http://localhost:8080/qsoft/procedure/dnspod_update \
  -H "Content-Type: application/json" \
  -d '[{"clientIp":"115.48.61.145","domain":"test.com"}]'

# 查询表数据
curl "http://localhost:8080/qsoft/table/Users?fields=Id,Name&limit=10&order=Id asc"

# 健康检查
curl "http://localhost:8080/api/dbaccess/ping?db=qsoft"
```

## 🔧 配置选项

| 环境变量 | 默认值 | 说明 |
|---------|--------|------|
| `DBACCESS_MSSQL_SERVER` | `61.163.200.245` | SQL Server 地址 |
| `DBACCESS_MSSQL_USER` | `sa` | 数据库用户名 |
| `DBACCESS_MSSQL_PASSWORD` | `GalaxyS24` | 数据库密码 |
| `DBACCESS_API_KEY` | 空 | API 密钥（可选） |
| `DBACCESS_SQL_WHITELIST` | `false` | 启用白名单模式 |
| `DBACCESS_SQL_STRICT_WHERE` | `false` | 严格 WHERE 解析 |
| `DBACCESS_MAX_PAGE_SIZE` | `1000` | 最大分页大小 |
| `DBACCESS_MAX_OFFSET` | `1000000` | 最大偏移量 |

## 🛡️ 安全特性

- **SQL 注入防护**：内置危险关键字拦截
- **参数校验**：严格的输入格式验证
- **白名单模式**：可选的表/列/函数白名单
- **分页限制**：防止过大查询
- **API 密钥**：可选的接口鉴权
- **CORS 支持**：配置跨域访问

## 📊 监控与管理

### 健康检查
```bash
# Docker 容器健康状态
docker ps
docker exec dbaccess-api curl -f http://localhost:8080/api/dbaccess/ping?db=master

# 服务日志
docker-compose logs -f dbaccess-api
```

### 容器管理
```bash
# 停止服务
docker-compose down

# 重启服务
docker-compose restart

# 更新服务
docker-compose pull && docker-compose up -d
```

## 🏗️ 生产部署建议

1. **资源限制**：已配置内存（512MB）和 CPU（0.5 核心）限制
2. **非 root 用户**：容器内使用 `dbaccess` 用户运行
3. **健康检查**：30秒间隔自动检查服务状态  
4. **日志管理**：建议配置日志轮转和外部日志收集
5. **网络安全**：生产环境建议使用 HTTPS 和防火墙
6. **数据库连接**：建议使用连接池和重试机制

## 🐛 故障排除

### 常见问题

1. **容器无法启动**
   ```bash
   docker-compose logs dbaccess-api
   ```

2. **数据库连接失败**
   - 检查网络连通性
   - 验证数据库凭据
   - 确认防火墙设置

3. **API 调用失败**
   - 检查请求方法（POST/GET）
   - 验证 Content-Type 头
   - 查看容器日志

### 调试模式

如需启用开发模式（包含 Swagger UI）：

```yaml
# docker-compose.yml
environment:
  - ASPNETCORE_ENVIRONMENT=Development
```

然后访问：http://localhost:8080/swagger

## 📝 版本信息

- **.NET**: 8.0 (LTS)
- **基础镜像**: Ubuntu (mcr.microsoft.com/dotnet/aspnet:8.0-ubuntu)
- **架构**: Multi-stage Docker build
- **运行时**: 非 root 用户，生产优化

## 🤝 支持

如有问题或建议，请检查：
1. 容器日志：`docker-compose logs dbaccess-api`
2. 健康检查：`curl http://localhost:8080/api/dbaccess/ping?db=master`
3. 网络连通性：`docker exec -it dbaccess-api ping your-db-server`