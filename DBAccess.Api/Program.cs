using DBAccess.Api.Services;
using DBAccess.Api.Services.Security;
using DBAccess.Api.Services.Email;
using DBAccess.Api.Services.Monitoring;
using Microsoft.Extensions.Options;
using DBAccess.Api.Json;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
// 作为 Windows 服务运行
builder.Host.UseWindowsService(options =>
{
    options.ServiceName = "DBAccess.Api";
});
if (OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}

// 配置 Kestrel 服务器从配置文件读取设置
builder.WebHost.ConfigureKestrel((context, options) =>
{
    var config = context.Configuration;
    var hostSettings = config.GetSection("HostSettings");
    var httpPort = int.Parse(hostSettings["HttpPort"] ?? "5189");
    var httpsPort = int.Parse(hostSettings["HttpsPort"] ?? "5190");
    
    // 配置 HTTP 监听 - 监听所有网络接口
    options.ListenAnyIP(httpPort);
    Console.WriteLine($"✅ HTTP enabled on all interfaces: 0.0.0.0:{httpPort}");
    
    // 配置 HTTPS 监听
    var certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
    string certPath = "";
    
    // 按优先级检查证书路径
    var certPaths = new[]
    {
        "/app/certificates/qsgl.net.pfx",
        "/certificates/qsgl.net.pfx",
        "./certificates/qsgl.net.pfx"
    };
    
    foreach (var path in certPaths)
    {
        if (File.Exists(path))
        {
            certPath = path;
            break;
        }
    }
    
    if (!string.IsNullOrEmpty(certPath) && !string.IsNullOrEmpty(certPassword))
    {
        try
        {
            options.ListenAnyIP(httpsPort, listenOptions =>
            {
                listenOptions.UseHttps(certPath, certPassword);
            });
            Console.WriteLine($"✅ HTTPS enabled with certificate: {certPath}");
            Console.WriteLine($"✅ HTTPS listening on all interfaces: 0.0.0.0:{httpsPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ HTTPS configuration failed: {ex.Message}");
            Console.WriteLine("⚠️  Falling back to HTTP-only mode");
        }
    }
    else
    {
        Console.WriteLine("⚠️  HTTPS disabled - certificate or password not found");
        Console.WriteLine($"   Certificate path checked: {string.Join(", ", certPaths)}");
        Console.WriteLine($"   CERT_PASSWORD environment variable: {(string.IsNullOrEmpty(certPassword) ? "NOT SET" : "SET")}");
    }
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 修复.NET 8 AOT的JSON序列化问题
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = false;
        options.JsonSerializerOptions.AllowTrailingCommas = true;
        options.JsonSerializerOptions.ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip;
        
        // 添加AOT兼容的JSON序列化上下文
        options.JsonSerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
    });
builder.Services.AddEndpointsApiExplorer();

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DBAccess REST API",
        Version = "v1",
        Description = "通用数据库访问API - 支持动态表查询、分页、排序和安全过滤",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "DBAccess API",
            Url = new Uri("https://tx.qsgl.net:5190")
        }
    });

    // API Key（X-API-Key）安全定义，便于在 UI 中 Authorize
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey,
        Description = "可选：在请求头中提供 X-API-Key"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure DbService with env overrides and provided defaults
var server = Environment.GetEnvironmentVariable("DBACCESS_MSSQL_SERVER") ?? "61.163.200.245";
var user = Environment.GetEnvironmentVariable("DBACCESS_MSSQL_USER") ?? "sa";
var password = Environment.GetEnvironmentVariable("DBACCESS_MSSQL_PASSWORD") ?? "GalaxyS24";
// SqlGuard default: 关闭白名单，但拦截明显危险关键字；可通过环境变量开启白名单
var guard = new SqlGuardOptions
{
    EnableWhitelist = (Environment.GetEnvironmentVariable("DBACCESS_SQL_WHITELIST") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase),
    UseStrictWhereParser = (Environment.GetEnvironmentVariable("DBACCESS_SQL_STRICT_WHERE") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase)
};
// 可选：通过分号分隔注入白名单列表
void LoadSet(string? env, HashSet<string> set)
{
    if (string.IsNullOrWhiteSpace(env)) return;
    foreach (var item in env.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        set.Add(item);
}
LoadSet(Environment.GetEnvironmentVariable("DBACCESS_ALLOWED_TABLES"), guard.AllowedTables);
LoadSet(Environment.GetEnvironmentVariable("DBACCESS_ALLOWED_FUNCTIONS"), guard.AllowedFunctions);
LoadSet(Environment.GetEnvironmentVariable("DBACCESS_ALLOWED_COLUMNS"), guard.AllowedColumns);

// 分页限制来自环境变量（可选）
if (int.TryParse(Environment.GetEnvironmentVariable("DBACCESS_MAX_PAGE_SIZE"), out var maxPage) && maxPage > 0)
    guard.MaxPageSize = maxPage;
if (int.TryParse(Environment.GetEnvironmentVariable("DBACCESS_MAX_OFFSET"), out var maxOff) && maxOff >= 0)
    guard.MaxOffset = maxOff;

builder.Services.AddSingleton(guard);
builder.Services.AddSingleton(new DbService(server, user, password, guard));

// SMTP & Monitor options from configuration/env
builder.Services.Configure<SmtpOptions>(opt =>
{
    opt.Host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? opt.Host;
    if (int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p)) opt.Port = p;
    opt.UseSsl = (Environment.GetEnvironmentVariable("SMTP_SSL") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
    opt.Username = Environment.GetEnvironmentVariable("SMTP_USERNAME") ?? opt.Username;
    opt.Password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? opt.Password;
    opt.From = Environment.GetEnvironmentVariable("SMTP_FROM") ?? opt.From ?? opt.Username;
    opt.To = Environment.GetEnvironmentVariable("ALERT_EMAIL_TO") ?? opt.To;
});
builder.Services.Configure<HealthMonitorOptions>(opt =>
{
    opt.Enabled = (Environment.GetEnvironmentVariable("MONITOR_ENABLED") ?? "true").Equals("true", StringComparison.OrdinalIgnoreCase);
    opt.HealthUrl = Environment.GetEnvironmentVariable("MONITOR_HEALTH_URL") ?? opt.HealthUrl;
    if (int.TryParse(Environment.GetEnvironmentVariable("MONITOR_INTERVAL_SEC"), out var iv)) opt.IntervalSeconds = iv;
    if (int.TryParse(Environment.GetEnvironmentVariable("MONITOR_FAIL_THRESHOLD"), out var ft)) opt.FailureThreshold = ft;
    if (int.TryParse(Environment.GetEnvironmentVariable("MONITOR_RECOVER_THRESHOLD"), out var rt)) opt.RecoveryThreshold = rt;
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<HealthCheckWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
// HTTPS重定向已禁用 - 仅运行HTTP服务

// 启用 CORS
app.UseCors();

// 启用 Swagger 文档（支持开发和生产环境）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBAccess REST API v1");
    c.RoutePrefix = "swagger";
    c.DefaultModelsExpandDepth(-1);
    c.DocumentTitle = "DBAccess API Documentation";
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

// 添加根路径重定向到 Swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

// 静态文件支持
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

// Ping 接口：快速检测指定 DBName 是否可连接
app.MapGet("/api/dbaccess/ping", (string db, DbService svc) =>
{
    try
    {
        // 选择一个极轻量的查询（使用分页限制而不是 TOP 表达式）
        var result = svc.Table(db, "sys.objects", "1=0", "*", "name asc", "1", "0");
        return Results.Text("{\"ok\":true}", "application/json");
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
