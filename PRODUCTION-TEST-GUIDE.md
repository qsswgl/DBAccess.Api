# 🧪 DBAccess.Api 生产环境测试指南

## 📋 测试概述

本文档提供完整的生产环境测试步骤和验证清单，确保 DBAccess.Api 在生产环境中正常运行。

**测试环境**: https://tx.qsgl.net:5190  
**测试时间**: 2025年10月24日  
**文档版本**: 1.0

---

## ✅ 测试前准备

### 1. 工具准备

#### Windows 环境
```powershell
# 检查 PowerShell 版本（建议 7.0+）
$PSVersionTable.PSVersion

# 检查 curl（Windows 10+ 自带）
curl --version

# 安装 PowerShell 7（可选，支持更多功能）
winget install Microsoft.PowerShell
```

#### Linux/Mac 环境
```bash
# 检查 curl
curl --version

# 安装 jq（用于 JSON 格式化）
# Ubuntu/Debian
sudo apt-get install jq

# Mac
brew install jq
```

### 2. 网络检查

```bash
# 检查域名解析
nslookup tx.qsgl.net

# 检查端口连通性
# Windows PowerShell
Test-NetConnection -ComputerName tx.qsgl.net -Port 5190

# Linux/Mac
nc -zv tx.qsgl.net 5190
telnet tx.qsgl.net 5190
```

### 3. 预期结果模板

创建一个测试结果记录表：
```
测试项 | 预期结果 | 实际结果 | 状态 | 备注
------|---------|---------|------|------
网络连通性 | 可访问 | | ⏳ |
Swagger UI | 正常显示 | | ⏳ |
健康检查 | 200 OK | | ⏳ |
...
```

---

## 🔍 详细测试步骤

### 测试 1: 基础连通性测试

#### 1.1 HTTP 端口测试

```bash
# 测试 HTTP 80 端口
curl -I http://tx.qsgl.net/swagger/index.html

# 预期结果：
# HTTP/1.1 200 OK
# Content-Type: text/html
```

```bash
# 测试 HTTP 5189 端口
curl -I http://tx.qsgl.net:5189/swagger/index.html

# 预期结果：
# HTTP/1.1 200 OK
# Content-Type: text/html
```

#### 1.2 HTTPS 端口测试

```bash
# 测试 HTTPS 5190 端口（推荐使用）
curl -I -k https://tx.qsgl.net:5190/swagger/index.html

# 预期结果：
# HTTP/2 200
# content-type: text/html
# server: Kestrel
```

**验证标准**:
- ✅ 返回状态码 200
- ✅ 响应头包含 content-type
- ✅ HTTPS 使用有效证书

**故障处理**:
- 如果返回 000：检查防火墙和网络
- 如果返回 404：检查 URL 路径
- 如果返回 500：查看服务器日志

---

### 测试 2: Swagger UI 界面测试

#### 2.1 浏览器访问

```
URL: https://tx.qsgl.net:5190/swagger/index.html
```

**检查项目**:
- [ ] 页面正常加载（无白屏）
- [ ] Swagger UI 标题显示正确
- [ ] API 列表完整显示
- [ ] 可展开 API 端点
- [ ] "Try it out" 按钮可用
- [ ] Schema 定义正确显示

#### 2.2 API 文档完整性

在 Swagger 界面中验证：

1. **健康检查端点**
   - `GET /api/dbaccess/ping`
   - 参数: db (query, string)

2. **存储过程调用端点**
   - `POST /Qsoft/procedure/{procedureName}`
   - 参数: procedureName (path, string)
   - 请求体: ProcedureInputModel

3. **输入参数字段**
   - Client_IP (string)
   - domain (string)
   - sub_domain (string)

**截图保存**: 建议截图保存 Swagger 界面以供记录

---

### 测试 3: 健康检查端点测试

#### 3.1 HTTP 健康检查

```bash
# 使用 master 数据库测试
curl -X GET "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master" \
  -H "accept: application/json"

# 预期响应示例：
{
  "status": "healthy",
  "database": "master",
  "timestamp": "2025-10-24T..."
}
```

#### 3.2 HTTPS 健康检查

```bash
# HTTPS 测试（推荐）
curl -X GET "https://tx.qsgl.net:5190/api/dbaccess/ping?db=master" \
  -H "accept: application/json" \
  -k

# 或使用 PowerShell（Windows）
$response = Invoke-RestMethod -Uri "https://tx.qsgl.net:5190/api/dbaccess/ping?db=master"
$response | ConvertTo-Json
```

#### 3.3 不同数据库测试

```bash
# 测试不同数据库
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=tempdb"
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=msdb"
curl "http://tx.qsgl.net:5189/api/dbaccess/ping?db=model"
```

**验证标准**:
- ✅ 所有请求返回 200 状态码
- ✅ 响应为有效 JSON
- ✅ 包含 status、database、timestamp 字段
- ✅ 响应时间 < 1000ms

**性能基准**:
```bash
# 测量响应时间
curl -o /dev/null -s -w "Time: %{time_total}s\n" \
  "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"

# 预期: Time: < 1.0s
```

---

### 测试 4: 存储过程调用测试

#### 4.1 DNSPOD_UPDATE 基础测试

```bash
# 测试用例 1: 正常参数
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "accept: application/json" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "115.48.63.233",
    "domain": "qsgl",
    "sub_domain": "test"
  }'

# 预期响应:
{
  "code": 0,
  "message": "执行成功",
  "data": "解析成功,域名test.qsgl已解析到:115.48.63.233"
}
```

#### 4.2 参数验证测试

```bash
# 测试用例 2: 空参数
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "",
    "domain": "",
    "sub_domain": ""
  }'

# 预期: 返回业务错误或参数验证错误

# 测试用例 3: 缺少参数
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "115.48.63.233"
  }'

# 预期: 正常处理（其他参数为 null）
```

#### 4.3 不同 IP 地址测试

```bash
# IPv4 测试
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "192.168.1.100",
    "domain": "qsgl",
    "sub_domain": "dev"
  }'

# 公网 IP 测试
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "8.8.8.8",
    "domain": "qsgl",
    "sub_domain": "public"
  }'
```

#### 4.4 PowerShell 测试脚本

```powershell
# 创建测试脚本 test-dbaccess.ps1

function Test-DBAccessAPI {
    param(
        [string]$BaseUrl = "https://tx.qsgl.net:5190",
        [string]$ClientIP = "115.48.63.233",
        [string]$Domain = "qsgl",
        [string]$SubDomain = "test"
    )

    $body = @{
        Client_IP = $ClientIP
        domain = $Domain
        sub_domain = $SubDomain
    } | ConvertTo-Json

    $uri = "$BaseUrl/Qsoft/procedure/DNSPOD_UPDATE"

    try {
        Write-Host "Testing: $uri" -ForegroundColor Cyan
        Write-Host "Body: $body" -ForegroundColor Gray

        $response = Invoke-RestMethod -Uri $uri `
            -Method Post `
            -ContentType "application/json" `
            -Body $body

        Write-Host "✅ Success!" -ForegroundColor Green
        $response | ConvertTo-Json -Depth 5
        return $true
    }
    catch {
        Write-Host "❌ Failed!" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

# 执行测试
Test-DBAccessAPI
Test-DBAccessAPI -SubDomain "dev"
Test-DBAccessAPI -SubDomain "prod"
```

**运行脚本**:
```powershell
.\test-dbaccess.ps1
```

---

### 测试 5: 并发和性能测试

#### 5.1 简单并发测试

```bash
# 并发 10 个请求
for i in {1..10}; do
  curl -X GET "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master" \
    -s -o /dev/null -w "Request $i: %{http_code} - %{time_total}s\n" &
done
wait

# 预期: 所有请求返回 200，响应时间相近
```

#### 5.2 Apache Bench 压力测试

```bash
# 安装 Apache Bench (ab)
# Ubuntu: sudo apt-get install apache2-utils
# Mac: brew install httpd

# 执行压力测试
ab -n 1000 -c 10 -g results.tsv \
  "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"

# 参数说明:
# -n 1000: 总共 1000 个请求
# -c 10: 并发 10 个
# -g results.tsv: 输出结果到文件
```

**关键指标**:
```
Concurrency Level:        10
Time taken for tests:     < 10 seconds
Complete requests:        1000
Failed requests:          0
Requests per second:      > 100 [#/sec]
Time per request:         < 100 [ms] (mean)
Transfer rate:            > 50 [Kbytes/sec]
```

#### 5.3 PowerShell 并发测试

```powershell
# 并发测试脚本
$jobs = 1..20 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($index)
        $uri = "http://tx.qsgl.net:5189/api/dbaccess/ping?db=master"
        $start = Get-Date
        try {
            $response = Invoke-RestMethod -Uri $uri -TimeoutSec 30
            $duration = ((Get-Date) - $start).TotalMilliseconds
            [PSCustomObject]@{
                Index = $index
                Status = "Success"
                Duration = $duration
                Response = $response
            }
        }
        catch {
            [PSCustomObject]@{
                Index = $index
                Status = "Failed"
                Error = $_.Exception.Message
            }
        }
    } -ArgumentList $_
}

# 等待所有任务完成
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

# 显示结果
$results | Format-Table -AutoSize
$results | Measure-Object -Property Duration -Average -Maximum -Minimum
```

---

### 测试 6: 错误处理测试

#### 6.1 无效端点测试

```bash
# 测试不存在的端点
curl -I https://tx.qsgl.net:5190/api/nonexistent

# 预期: 404 Not Found
```

#### 6.2 无效 HTTP 方法

```bash
# 使用错误的 HTTP 方法
curl -X DELETE https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE

# 预期: 405 Method Not Allowed
```

#### 6.3 无效 JSON 格式

```bash
# 发送格式错误的 JSON
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d 'invalid json{}'

# 预期: 400 Bad Request
```

#### 6.4 缺少 Content-Type

```bash
# 不指定 Content-Type
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -k \
  -d '{"Client_IP":"1.1.1.1","domain":"test","sub_domain":"api"}'

# 预期: 可能返回 415 Unsupported Media Type 或正常处理
```

---

### 测试 7: 安全性测试

#### 7.1 HTTPS 证书验证

```bash
# 检查证书详情
openssl s_client -connect tx.qsgl.net:5190 -servername tx.qsgl.net < /dev/null 2>/dev/null | \
  openssl x509 -noout -text

# 检查项:
# - Subject: CN=*.qsgl.net
# - Issuer: 证书颁发机构
# - Validity: 有效期
# - Subject Alternative Name: DNS:*.qsgl.net
```

#### 7.2 SQL 注入测试

```bash
# 尝试 SQL 注入
curl -X POST "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Content-Type: application/json" \
  -k \
  -d '{
    "Client_IP": "1.1.1.1; DROP TABLE users;--",
    "domain": "qsgl",
    "sub_domain": "test"
  }'

# 预期: 应该安全处理，不执行注入代码
```

#### 7.3 超大请求测试

```bash
# 发送超大数据
python3 -c "
import requests
import json

data = {
    'Client_IP': 'A' * 10000,
    'domain': 'B' * 10000,
    'sub_domain': 'C' * 10000
}

response = requests.post(
    'https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE',
    json=data,
    verify=False
)
print(f'Status: {response.status_code}')
print(response.text[:200])
"

# 预期: 应该有合理的请求大小限制
```

---

### 测试 8: 跨域 (CORS) 测试

#### 8.1 OPTIONS 预检请求

```bash
curl -X OPTIONS "https://tx.qsgl.net:5190/Qsoft/procedure/DNSPOD_UPDATE" \
  -H "Origin: https://example.com" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: Content-Type" \
  -k -I

# 检查响应头中的 CORS 设置
```

#### 8.2 JavaScript 跨域请求

```javascript
// 在浏览器控制台中测试
fetch('https://tx.qsgl.net:5190/api/dbaccess/ping?db=master', {
    method: 'GET',
    headers: {
        'Accept': 'application/json'
    }
})
.then(response => response.json())
.then(data => console.log('Success:', data))
.catch(error => console.error('Error:', error));
```

---

## 📊 测试结果记录模板

### 测试执行记录表

```
测试日期: 2025-10-24
测试人员: ___________
环境: 生产环境 (tx.qsgl.net)

┌─────────────────────────┬──────────┬──────────┬────────┬─────────┐
│ 测试项                  │ 预期结果 │ 实际结果 │ 状态   │ 备注    │
├─────────────────────────┼──────────┼──────────┼────────┼─────────┤
│ 1. 网络连通性           │ 可访问   │          │ ⏳/✅/❌│         │
│ 2. Swagger UI          │ 正常显示 │          │ ⏳/✅/❌│         │
│ 3. HTTP 80端口         │ 200 OK   │          │ ⏳/✅/❌│         │
│ 4. HTTP 5189端口       │ 200 OK   │          │ ⏳/✅/❌│         │
│ 5. HTTPS 5190端口      │ 200 OK   │          │ ⏳/✅/❌│         │
│ 6. 健康检查-master     │ 200 OK   │          │ ⏳/✅/❌│         │
│ 7. 健康检查-tempdb     │ 200 OK   │          │ ⏳/✅/❌│         │
│ 8. DNSPOD_UPDATE调用   │ 成功执行 │          │ ⏳/✅/❌│         │
│ 9. 参数验证            │ 正确处理 │          │ ⏳/✅/❌│         │
│ 10. 错误处理           │ 合理响应 │          │ ⏳/✅/❌│         │
│ 11. 并发测试(10)       │ 全部成功 │          │ ⏳/✅/❌│         │
│ 12. 性能测试           │ <1000ms  │          │ ⏳/✅/❌│         │
│ 13. SSL证书            │ 有效     │          │ ⏳/✅/❌│         │
│ 14. JSON响应格式       │ 正确     │          │ ⏳/✅/❌│         │
│ 15. 超时处理           │ 合理     │          │ ⏳/✅/❌│         │
└─────────────────────────┴──────────┴──────────┴────────┴─────────┘

总体评估: ⏳ 待测试 / ✅ 通过 / ❌ 失败

问题记录:
1. _______________________________________________________________
2. _______________________________________________________________
3. _______________________________________________________________

建议:
1. _______________________________________________________________
2. _______________________________________________________________
3. _______________________________________________________________

测试签名: ___________  日期: ___________
```

---

## 🔧 自动化测试脚本

### 完整测试套件 (Bash)

创建文件 `production-test-suite.sh`:

```bash
#!/bin/bash

# DBAccess.Api 生产环境测试套件
# 使用方法: ./production-test-suite.sh

BASE_URL="https://tx.qsgl.net:5190"
HTTP_URL="http://tx.qsgl.net:5189"

echo "🧪 DBAccess.Api 生产环境测试"
echo "================================"
echo ""

# 颜色定义
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

PASS_COUNT=0
FAIL_COUNT=0

# 测试函数
test_endpoint() {
    local name=$1
    local url=$2
    local expected_code=$3
    
    echo -n "测试: $name ... "
    
    http_code=$(curl -s -o /dev/null -w "%{http_code}" -k "$url")
    
    if [ "$http_code" -eq "$expected_code" ]; then
        echo -e "${GREEN}✅ PASS${NC} (HTTP $http_code)"
        ((PASS_COUNT++))
        return 0
    else
        echo -e "${RED}❌ FAIL${NC} (预期 $expected_code, 实际 $http_code)"
        ((FAIL_COUNT++))
        return 1
    fi
}

test_post_endpoint() {
    local name=$1
    local url=$2
    local data=$3
    
    echo -n "测试: $name ... "
    
    response=$(curl -s -w "\n%{http_code}" -k -X POST "$url" \
        -H "Content-Type: application/json" \
        -d "$data")
    
    http_code=$(echo "$response" | tail -n1)
    body=$(echo "$response" | head -n-1)
    
    if [ "$http_code" -eq 200 ]; then
        echo -e "${GREEN}✅ PASS${NC}"
        echo "   响应: $body" | head -c 100
        echo ""
        ((PASS_COUNT++))
        return 0
    else
        echo -e "${RED}❌ FAIL${NC} (HTTP $http_code)"
        ((FAIL_COUNT++))
        return 1
    fi
}

# 开始测试
echo "📡 1. 连通性测试"
echo "----------------"
test_endpoint "HTTPS Swagger" "$BASE_URL/swagger/index.html" 200
test_endpoint "HTTP Swagger" "$HTTP_URL/swagger/index.html" 200
echo ""

echo "💓 2. 健康检查测试"
echo "----------------"
test_endpoint "健康检查-master" "$HTTP_URL/api/dbaccess/ping?db=master" 200
test_endpoint "健康检查-tempdb" "$HTTP_URL/api/dbaccess/ping?db=tempdb" 200
echo ""

echo "🔧 3. API 功能测试"
echo "----------------"
test_post_endpoint "DNSPOD_UPDATE" "$BASE_URL/Qsoft/procedure/DNSPOD_UPDATE" \
    '{"Client_IP":"115.48.63.233","domain":"qsgl","sub_domain":"test"}'
echo ""

echo "⚡ 4. 性能测试"
echo "----------------"
echo -n "响应时间测试 ... "
time_total=$(curl -o /dev/null -s -w "%{time_total}" -k "$HTTP_URL/api/dbaccess/ping?db=master")
if (( $(echo "$time_total < 1.0" | bc -l) )); then
    echo -e "${GREEN}✅ PASS${NC} (${time_total}s)"
    ((PASS_COUNT++))
else
    echo -e "${YELLOW}⚠️  SLOW${NC} (${time_total}s)"
fi
echo ""

# 测试总结
echo "================================"
echo "📊 测试总结"
echo "================================"
echo -e "通过: ${GREEN}$PASS_COUNT${NC}"
echo -e "失败: ${RED}$FAIL_COUNT${NC}"
echo "总计: $((PASS_COUNT + FAIL_COUNT))"
echo ""

if [ $FAIL_COUNT -eq 0 ]; then
    echo -e "${GREEN}🎉 所有测试通过！${NC}"
    exit 0
else
    echo -e "${RED}❌ 有 $FAIL_COUNT 个测试失败${NC}"
    exit 1
fi
```

**运行测试**:
```bash
chmod +x production-test-suite.sh
./production-test-suite.sh
```

### PowerShell 测试套件

创建文件 `production-test-suite.ps1`:

```powershell
# DBAccess.Api 生产环境测试套件 (PowerShell)

$BaseUrl = "https://tx.qsgl.net:5190"
$HttpUrl = "http://tx.qsgl.net:5189"

$PassCount = 0
$FailCount = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$ExpectedCode = 200
    )
    
    Write-Host -NoNewline "测试: $Name ... "
    
    try {
        $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -ErrorAction Stop
        
        if ($response.StatusCode -eq $ExpectedCode) {
            Write-Host "✅ PASS" -ForegroundColor Green -NoNewline
            Write-Host " (HTTP $($response.StatusCode))"
            $script:PassCount++
            return $true
        }
    }
    catch {
        Write-Host "❌ FAIL" -ForegroundColor Red -NoNewline
        Write-Host " ($($_.Exception.Message))"
        $script:FailCount++
        return $false
    }
}

function Test-PostEndpoint {
    param(
        [string]$Name,
        [string]$Url,
        [hashtable]$Body
    )
    
    Write-Host -NoNewline "测试: $Name ... "
    
    try {
        $json = $Body | ConvertTo-Json
        $response = Invoke-RestMethod -Uri $Url -Method Post -Body $json -ContentType "application/json"
        
        Write-Host "✅ PASS" -ForegroundColor Green
        Write-Host "   响应: $($response | ConvertTo-Json -Compress)" -ForegroundColor Gray
        $script:PassCount++
        return $true
    }
    catch {
        Write-Host "❌ FAIL" -ForegroundColor Red
        Write-Host "   错误: $($_.Exception.Message)" -ForegroundColor Red
        $script:FailCount++
        return $false
    }
}

# 开始测试
Write-Host ""
Write-Host "🧪 DBAccess.Api 生产环境测试" -ForegroundColor Cyan
Write-Host "================================"
Write-Host ""

Write-Host "📡 1. 连通性测试" -ForegroundColor Yellow
Write-Host "----------------"
Test-Endpoint "HTTP Swagger" "$HttpUrl/swagger/index.html"
Write-Host ""

Write-Host "💓 2. 健康检查测试" -ForegroundColor Yellow
Write-Host "----------------"
Test-Endpoint "健康检查-master" "$HttpUrl/api/dbaccess/ping?db=master"
Test-Endpoint "健康检查-tempdb" "$HttpUrl/api/dbaccess/ping?db=tempdb"
Write-Host ""

Write-Host "🔧 3. API 功能测试" -ForegroundColor Yellow
Write-Host "----------------"
Test-PostEndpoint "DNSPOD_UPDATE" "$BaseUrl/Qsoft/procedure/DNSPOD_UPDATE" @{
    Client_IP = "115.48.63.233"
    domain = "qsgl"
    sub_domain = "test"
}
Write-Host ""

# 测试总结
Write-Host "================================"
Write-Host "📊 测试总结" -ForegroundColor Cyan
Write-Host "================================"
Write-Host "通过: " -NoNewline
Write-Host $PassCount -ForegroundColor Green
Write-Host "失败: " -NoNewline
Write-Host $FailCount -ForegroundColor Red
Write-Host "总计: $($PassCount + $FailCount)"
Write-Host ""

if ($FailCount -eq 0) {
    Write-Host "🎉 所有测试通过！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ 有 $FailCount 个测试失败" -ForegroundColor Red
    exit 1
}
```

**运行测试**:
```powershell
.\production-test-suite.ps1
```

---

## 📝 测试报告示例

### 测试报告模板

```markdown
# DBAccess.Api 生产环境测试报告

**测试日期**: 2025-10-24  
**测试人员**: [姓名]  
**测试环境**: 生产环境 (tx.qsgl.net)  
**测试时长**: [X] 分钟

## 执行摘要

- 总测试用例数: 15
- 通过: 14 ✅
- 失败: 1 ❌
- 跳过: 0 ⏭️
- 通过率: 93.3%

## 详细结果

### ✅ 通过的测试 (14)

1. HTTPS Swagger UI 访问 - 200 OK
2. HTTP 端口 5189 连通性 - 200 OK
3. 健康检查 master 数据库 - 响应时间 234ms
4. 健康检查 tempdb 数据库 - 响应时间 189ms
5. DNSPOD_UPDATE 正常调用 - 业务逻辑正确
... (其他通过的测试)

### ❌ 失败的测试 (1)

1. **容器健康检查状态**
   - 预期: healthy
   - 实际: unhealthy
   - 原因: 健康检查端点 /api/dbaccess/ping 在容器内无法访问
   - 影响: 低 - API实际功能正常,仅监控状态显示异常
   - 建议: 修改健康检查配置或端点路径

## 性能指标

- 平均响应时间: 245ms
- 最快响应: 123ms (健康检查)
- 最慢响应: 567ms (存储过程调用)
- 并发10请求成功率: 100%

## 发现的问题

### P2 - 健康检查显示 unhealthy
- 描述: Docker容器健康检查始终显示unhealthy
- 重现步骤: docker inspect dbaccess-api
- 影响范围: 监控告警
- 建议措施: 更新健康检查命令或禁用

## 结论

✅ **测试通过** - API服务在生产环境运行正常,可以投入使用。

虽然容器健康检查显示异常,但实际API功能完全正常,所有核心业务测试均通过。
建议后续优化健康检查配置。

## 签字

测试人员: ___________  日期: ___________  
审核人员: ___________  日期: ___________
```

---

## 🚨 故障应急手册

### 如果测试失败...

#### 场景 1: 无法访问 Swagger

```bash
# 1. 检查服务器状态
ssh root@tx.qsgl.net "systemctl status docker"

# 2. 检查容器状态
ssh root@tx.qsgl.net "docker ps | grep dbaccess"

# 3. 查看容器日志
ssh root@tx.qsgl.net "docker logs --tail 100 dbaccess-api"

# 4. 重启容器
ssh root@tx.qsgl.net "docker restart dbaccess-api"
```

#### 场景 2: API 返回 500 错误

```bash
# 查看详细错误日志
ssh root@tx.qsgl.net "docker logs dbaccess-api 2>&1 | grep -i exception"

# 检查数据库连接
ssh root@tx.qsgl.net "docker exec dbaccess-api printenv | grep DBACCESS"
```

#### 场景 3: 性能下降

```bash
# 检查资源使用
ssh root@tx.qsgl.net "docker stats dbaccess-api --no-stream"

# 检查系统负载
ssh root@tx.qsgl.net "top -bn1 | head -20"
```

---

## 📞 支持联系

**如有问题,请联系:**
- GitHub Issues: https://github.com/qsswgl/DBAccess.Api/issues
- 维护团队: qsswgl

**紧急联系 (生产故障):**
- 值班电话: [待补充]
- 邮件: [待补充]

---

**文档版本**: 1.0  
**最后更新**: 2025-10-24
