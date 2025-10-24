namespace DBAccess.Api.Services.Email
{
    public class SmtpOptions
    {
        public string Host { get; set; } = "smtp.139.com";
        public int Port { get; set; } = 465; // 465 SSL 或 587 STARTTLS
        public bool UseSsl { get; set; } = true;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? From { get; set; }
        public string To { get; set; } = "qsoft@139.com"; // 默认接收人
        public string? DisplayName { get; set; } = "DBAccess.Api Monitor";
        public int TimeoutMs { get; set; } = 15000;
    }
}
