using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Plafind.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? fromName = null, string? fromEmail = null);
        Task<bool> SendContactEmailAsync(string name, string email, string? phone, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, string? fromName = null, string? fromEmail = null)
        {
            try
            {
                var smtpHost = _configuration["EmailSettings:SmtpHost"];
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var smtpUsername = _configuration["EmailSettings:SmtpUsername"];
                var smtpPassword = _configuration["EmailSettings:SmtpPassword"]?.Replace(" ", ""); // Boşlukları temizle
                var defaultFromEmail = _configuration["EmailSettings:FromEmail"] ?? "noreply@plafind.com";
                var defaultFromName = _configuration["EmailSettings:FromName"] ?? "Alanya İşletme Rehberi";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUsername) || string.IsNullOrEmpty(smtpPassword))
                {
                    _logger.LogWarning("E-posta ayarları yapılandırılmamış. E-posta gönderilemedi.");
                    return false;
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName ?? defaultFromName, fromEmail ?? defaultFromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body
                };
                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUsername, smtpPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation($"E-posta başarıyla gönderildi: {toEmail}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"E-posta gönderilirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendContactEmailAsync(string name, string email, string? phone, string subject, string message)
        {
            try
            {
                var adminEmail = _configuration["EmailSettings:AdminEmail"] ?? _configuration["EmailSettings:FromEmail"] ?? "info@plafind.com";
                var siteName = _configuration["EmailSettings:SiteName"] ?? "Alanya İşletme Rehberi";

                // Admin'e gönderilecek e-posta içeriği
                var adminEmailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px; }}
        .info-row {{ margin: 10px 0; padding: 10px; background: white; border-radius: 4px; }}
        .label {{ font-weight: bold; color: #666; }}
        .message-box {{ background: white; padding: 15px; border-left: 4px solid #ffc107; margin: 15px 0; border-radius: 4px; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>Yeni İletişim Formu Mesajı</h2>
        </div>
        <div class='content'>
            <div class='info-row'>
                <span class='label'>Ad Soyad:</span> {name}
            </div>
            <div class='info-row'>
                <span class='label'>E-posta:</span> <a href='mailto:{email}'>{email}</a>
            </div>
            {(string.IsNullOrEmpty(phone) ? "" : $@"
            <div class='info-row'>
                <span class='label'>Telefon:</span> {phone}
            </div>")}
            <div class='info-row'>
                <span class='label'>Konu:</span> {subject}
            </div>
            <div class='message-box'>
                <strong>Mesaj:</strong><br>
                {message.Replace("\n", "<br>")}
            </div>
            <div class='footer'>
                <p>Bu mesaj {siteName} iletişim formundan gönderilmiştir.</p>
                <p>Yanıt vermek için: <a href='mailto:{email}'>Buraya tıklayın</a></p>
            </div>
        </div>
    </div>
</body>
</html>";

                // Kullanıcıya otomatik yanıt e-postası
                var userEmailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px; }}
        .message {{ background: white; padding: 15px; border-radius: 4px; margin: 15px 0; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>Mesajınız Alındı!</h2>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{name}</strong>,</p>
            <div class='message'>
                <p>Mesajınızı aldık ve en kısa sürede size dönüş yapacağız.</p>
                <p><strong>Mesajınız:</strong></p>
                <p style='background: #f5f5f5; padding: 10px; border-radius: 4px;'>{message.Replace("\n", "<br>")}</p>
            </div>
            <p>İyi günler dileriz,<br><strong>{siteName} Ekibi</strong></p>
            <div class='footer'>
                <p>Bu bir otomatik yanıt e-postasıdır. Lütfen bu e-postaya yanıt vermeyin.</p>
            </div>
        </div>
    </div>
</body>
</html>";

                // Admin'e e-posta gönder
                var adminEmailSent = await SendEmailAsync(
                    adminEmail,
                    $"[İletişim Formu] {subject}",
                    adminEmailBody,
                    siteName
                );

                // Kullanıcıya otomatik yanıt gönder
                var userEmailSent = await SendEmailAsync(
                    email,
                    $"Mesajınız Alındı - {siteName}",
                    userEmailBody,
                    siteName
                );

                return adminEmailSent && userEmailSent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"İletişim formu e-postası gönderilirken hata oluştu: {ex.Message}");
                return false;
            }
        }
    }
}

