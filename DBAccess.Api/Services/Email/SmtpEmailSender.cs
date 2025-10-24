using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.Extensions.Options;

namespace DBAccess.Api.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpOptions _options;

        public SmtpEmailSender(IOptions<SmtpOptions> options)
        {
            _options = options.Value;
        }

        public Task SendAsync(string subject, string htmlBody, string? toOverride = null, CancellationToken ct = default)
        {
            // SmtpClient 不完全异步；用 Task.Run 包装即可
            return Task.Run(() =>
            {
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.UseSsl,
                    Credentials = string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password)
                        ? null
                        : new NetworkCredential(_options.Username, _options.Password),
                    Timeout = _options.TimeoutMs
                };

                var fromAddress = _options.From ?? _options.Username ?? "noreply@example.com";
                var toAddress = toOverride ?? _options.To;

                using var msg = new MailMessage()
                {
                    From = new MailAddress(fromAddress, _options.DisplayName ?? "DBAccess.Api"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true,
                    BodyEncoding = Encoding.UTF8,
                    SubjectEncoding = Encoding.UTF8
                };
                msg.To.Add(new MailAddress(toAddress));

                client.Send(msg);
            }, ct);
        }
    }
}
