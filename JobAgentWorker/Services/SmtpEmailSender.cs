using System.Net;
using System.Net.Mail;

namespace JobAgentWorkerService.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            var smtp = _config.GetSection("Smtp");
            var host = smtp["Host"] ?? "localhost";
            var port = int.TryParse(smtp["Port"], out var p) ? p : 25;
            var username = smtp["Username"];
            var password = smtp["Password"];
            var from = smtp["From"] ?? "noreply@findjob.nu";
            var enableSsl = bool.TryParse(smtp["EnableSsl"], out var ssl) ? ssl : true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = string.IsNullOrEmpty(username) ? null : new NetworkCredential(username, password),
                Timeout = 15000
            };

            using var message = new MailMessage(from, toEmail, subject, htmlBody)
            {
                IsBodyHtml = true
            };

            _logger.LogInformation("Sending email to {ToEmail} via {Host}:{Port}", toEmail, host, port);
            await client.SendMailAsync(message, ct);
        }
    }
}
