using System.Net.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DBAccess.Api.Services.Email;

namespace DBAccess.Api.Services.Monitoring
{
    public class HealthMonitorOptions
    {
        public string HealthUrl { get; set; } = "http://localhost:8080/api/dbaccess/ping?db=master";
        public int IntervalSeconds { get; set; } = 30;
        public int FailureThreshold { get; set; } = 3;
        public int RecoveryThreshold { get; set; } = 2;
        public bool Enabled { get; set; } = true;
    }

    public class HealthCheckWorker : BackgroundService
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly ILogger<HealthCheckWorker> _logger;
        private readonly IEmailSender _emailSender;
        private readonly HealthMonitorOptions _options;

        private int _failureCount = 0;
        private int _successCount = 0;
        private bool _inAlert = false;

        public HealthCheckWorker(IHttpClientFactory httpFactory,
                                 ILogger<HealthCheckWorker> logger,
                                 IEmailSender emailSender,
                                 IOptions<HealthMonitorOptions> options)
        {
            _httpFactory = httpFactory;
            _logger = logger;
            _emailSender = emailSender;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation("HealthCheckWorker disabled by configuration.");
                return;
            }

            var interval = TimeSpan.FromSeconds(Math.Max(5, _options.IntervalSeconds));
            var client = _httpFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            _logger.LogInformation("HealthCheckWorker started. Interval: {Interval}s, Threshold: {Fail}/{Recover}", _options.IntervalSeconds, _options.FailureThreshold, _options.RecoveryThreshold);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var resp = await client.GetAsync(_options.HealthUrl, stoppingToken);
                    if (resp.IsSuccessStatusCode)
                    {
                        _successCount++;
                        _failureCount = 0;
                        _logger.LogDebug("Health OK ({Status})", (int)resp.StatusCode);

                        if (_inAlert && _successCount >= _options.RecoveryThreshold)
                        {
                            await NotifyAsync("DBAccess.Api 恢复正常",
                                $"<p>✅ 服务恢复正常</p><p>时间：{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}</p><p>健康检查：{_options.HealthUrl}</p>");
                            _inAlert = false;
                            _successCount = 0;
                        }
                    }
                    else
                    {
                        await HandleFailure($"HTTP {(int)resp.StatusCode} - {resp.ReasonPhrase}");
                    }
                }
                catch (Exception ex)
                {
                    await HandleFailure(ex.Message);
                }

                await Task.Delay(interval, stoppingToken);
            }

            async Task HandleFailure(string reason)
            {
                _failureCount++;
                _successCount = 0;
                _logger.LogWarning("Health check failed #{Count}: {Reason}", _failureCount, reason);

                if (!_inAlert && _failureCount >= _options.FailureThreshold)
                {
                    await NotifyAsync("DBAccess.Api 健康检查告警",
                        $"<p>❌ 服务健康检查失败</p><ul><li>失败次数：{_failureCount}</li><li>时间：{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}</li><li>原因：{System.Net.WebUtility.HtmlEncode(reason)}</li><li>检查地址：{_options.HealthUrl}</li></ul>");
                    _inAlert = true;
                }
            }

            async Task NotifyAsync(string subject, string body)
            {
                try
                {
                    await _emailSender.SendAsync(subject, body);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "发送告警邮件失败");
                }
            }
        }
    }
}
