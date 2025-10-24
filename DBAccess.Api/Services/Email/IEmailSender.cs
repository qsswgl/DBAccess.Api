using System.Threading.Tasks;

namespace DBAccess.Api.Services.Email
{
    public interface IEmailSender
    {
        Task SendAsync(string subject, string htmlBody, string? toOverride = null, CancellationToken ct = default);
    }
}
