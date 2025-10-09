using DBAccess.Api.Services;
using DBAccess.Api.Services.Security;
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
    // 清除任何默认配置，确保使用我们的设置
    
    var config = context.Configuration;
    var hostSettings = config.GetSection("HostSettings");
    var domain = hostSettings["Domain"] ?? "localhost";
    var httpPort = int.Parse(hostSettings["HttpPort"] ?? "5189");
    var httpsPort = int.Parse(hostSettings["HttpsPort"] ?? "5190");
    
    // 配置 HTTP 监听 - 监听所有网络接口
    options.ListenAnyIP(httpPort);
    Console.WriteLine($"✅ HTTP enabled on all interfaces: 0.0.0.0:{httpPort}");
    
    // 尝试配置 HTTPS
    var certPath = "certificates/qsgl.net.pfx";
    var certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD");
    
    var fullCertPath = Path.Combine(Directory.GetCurrentDirectory(), certPath);
    var enableHttps = File.Exists(fullCertPath) && !string.IsNullOrEmpty(certPassword);
    
    if (enableHttps)
    {
        // HTTPS - 监听所有网络接口，支持域名访问
        options.ListenAnyIP(httpsPort, listenOptions =>
        {
            try
            {
                listenOptions.UseHttps(fullCertPath, certPassword);
                Console.WriteLine($"✅ HTTPS enabled with certificate: {fullCertPath}");
                Console.WriteLine($"✅ HTTPS listening on all interfaces: 0.0.0.0:{httpsPort}");
                Console.WriteLine($"✅ Domain access: https://{domain}:{httpsPort}");
                Console.WriteLine($"✅ External access enabled for domain and IP addresses");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to load certificate: {ex.Message}");
                Console.WriteLine("   HTTPS will be disabled. Check certificate path and password.");
            }
        });
    }
    else
    {
        if (!File.Exists(fullCertPath))
        {
            Console.WriteLine($"⚠️  Certificate not found: {fullCertPath}");
        }
        if (string.IsNullOrEmpty(certPassword))
        {
            Console.WriteLine("⚠️  Certificate password not set (CERT_PASSWORD environment variable)");
        }
        Console.WriteLine("   Running in HTTP-only mode.");
    }
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
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
        Version = "v1"
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

var app = builder.Build();

// Configure the HTTP request pipeline.
// 检查是否启用 HTTPS 重定向（生产环境）
var hostSettings = app.Configuration.GetSection("HostSettings");
var enableHttpsRedirect = bool.Parse(hostSettings["EnableHttpsRedirect"] ?? "false");

if (enableHttpsRedirect && !app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    Console.WriteLine("✅ HTTPS redirection enabled for production");
}

// 启用 CORS
app.UseCors();

// 仅在开发环境启用 Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DBAccess REST v1");
        c.RoutePrefix = "swagger";
        c.DefaultModelsExpandDepth(-1);
    });
    
    // 开发环境的测试页面
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.MapControllers();

// Ping 接口：快速检测指定 DBName 是否可连接
app.MapGet("/api/dbaccess/ping", (string db, DbService svc) =>
{
    try
    {
        // 选择一个极轻量的查询（使用分页限制而不是 TOP 表达式）
        var result = svc.Table(db, "sys.objects", "1=0", "*", "name asc", "1", "0");
        return Results.Ok(new { ok = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
