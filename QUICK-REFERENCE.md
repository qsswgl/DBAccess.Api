# 🚀 DBAccess.Api 快速参考

## 📍 生产环境地址

```
HTTPS (推荐): https://tx.qsgl.net:5190
HTTP (备用):  http://tx.qsgl.net:5189
Swagger UI:   https://tx.qsgl.net:5190/swagger/index.html
```

---

## ⚡ 快速测试命令

### 健康检查
```bash
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"
```

### 调用存储过程 (DNSPOD_UPDATE)
```bash
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "115.48.63.233",
    "domain": "qsgl",
    "sub_domain": "test"
  }'
```

### PowerShell 版本
```powershell
$body = @{
    Client_IP = "115.48.63.233"
    domain = "qsgl"
    sub_domain = "test"
} | ConvertTo-Json

Invoke-RestMethod -Uri "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

---

## 🐳 Docker 管理命令

### 查看容器状态
```bash
ssh -i C:\Key\tx.qsgl.net_id_ed25519 root@tx.qsgl.net "docker ps | grep dbaccess"
```

### 查看日志
```bash
ssh -i C:\Key\tx.qsgl.net_id_ed25519 root@tx.qsgl.net "docker logs -f dbaccess-api"
```

### 重启容器
```bash
ssh -i C:\Key\tx.qsgl.net_id_ed25519 root@tx.qsgl.net "docker restart dbaccess-api"
```

### 查看健康状态
```bash
ssh -i C:\Key\tx.qsgl.net_id_ed25519 root@tx.qsgl.net \
  "docker inspect dbaccess-api --format='{{.State.Health.Status}}'"
```

---

## 📊 端口映射

| 外部端口 | 内部端口 | 协议 | 用途 |
|---------|---------|------|------|
| 80 | 8080 | HTTP | 标准访问 |
| 5189 | 8080 | HTTP | 备用端口 |
| 5190 | 8443 | HTTPS | **推荐使用** |

---

## 🔑 关键配置

### 环境变量
```bash
DBACCESS_MSSQL_SERVER=61.163.200.245
DBACCESS_MSSQL_USER=sa
DBACCESS_MSSQL_PASSWORD=GalaxyS24
CERT_PASSWORD=qsgl2024
```

### 证书路径
```
服务器: /opt/shared-certs/qsgl.net.pfx
容器内: /app/certificates/qsgl.net.pfx
```

---

## 📝 API 端点速查

### 1. 健康检查
```
GET /api/dbaccess/ping?db={数据库名}
```

### 2. 调用存储过程
```
POST /Qsoft/procedure/{存储过程名}
Content-Type: application/json

{
  "Client_IP": "IP地址",
  "domain": "域名",
  "sub_domain": "子域名"
}
```

---

## 🛠️ 故障快速排查

### 问题: API 无响应
```bash
# 1. 检查容器状态
docker ps | grep dbaccess

# 2. 查看日志
docker logs --tail 50 dbaccess-api

# 3. 重启容器
docker restart dbaccess-api
```

### 问题: HTTPS 不可用
```bash
# 检查证书
ls -la /opt/shared-certs/qsgl.net.pfx

# 检查证书挂载
docker inspect dbaccess-api | grep -A 5 Mounts
```

### 问题: 数据库连接失败
```bash
# 检查环境变量
docker exec dbaccess-api printenv | grep DBACCESS
```

---

## 📚 完整文档链接

- [项目完整文档](PROJECT-DOCUMENTATION.md)
- [生产环境测试指南](PRODUCTION-TEST-GUIDE.md)
- [故障排查文档](DBAccess.Api/DOMAIN-ACCESS-TROUBLESHOOTING.md)
- [GitHub 仓库](https://github.com/qsswgl/DBAccess.Api)

---

## 🔄 更新部署流程

```bash
# 1. 服务器拉取代码
cd /root/DBAccess.Api && git pull

# 2. 构建镜像
cd DBAccess.Api
docker build -f Dockerfile.net8 -t 43.138.35.183:5000/dbaccess-api:latest .

# 3. 停止旧容器
docker stop dbaccess-api && docker rm dbaccess-api

# 4. 启动新容器
docker run -d --name dbaccess-api --restart unless-stopped \
  -p 80:8080 -p 5189:8080 -p 5190:8443 \
  -v /opt/shared-certs:/app/certificates:ro \
  -e DBACCESS_MSSQL_SERVER=61.163.200.245 \
  -e DBACCESS_MSSQL_USER=sa \
  -e DBACCESS_MSSQL_PASSWORD=GalaxyS24 \
  -e CERT_PASSWORD=qsgl2024 \
  43.138.35.183:5000/dbaccess-api:latest

# 5. 验证
curl "http://localhost:8080/api/dbaccess/ping?db=master"
```

---

## 📞 支持

- **GitHub Issues**: https://github.com/qsswgl/DBAccess.Api/issues
- **维护者**: qsswgl

---

**最后更新**: 2025-10-24
