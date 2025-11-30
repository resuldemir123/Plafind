using Microsoft.Extensions.Configuration;

namespace Plafind.Services
{
    public interface ISmsService
    {
        Task<bool> SendSmsAsync(string phoneNumber, string message);
        Task<bool> SendPasswordResetCodeAsync(string phoneNumber, string code);
    }

    public class SmsService : ISmsService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IConfiguration configuration, ILogger<SmsService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendSmsAsync(string phoneNumber, string message)
        {
            try
            {
                var smsProvider = _configuration["SmsSettings:Provider"] ?? "Netgsm";
                
                // Telefon numarasını temizle (boşluk, tire, parantez vb. kaldır)
                phoneNumber = CleanPhoneNumber(phoneNumber);

                switch (smsProvider.ToLower())
                {
                    case "netgsm":
                        return await SendViaNetgsmAsync(phoneNumber, message);
                    case "twilio":
                        return await SendViaTwilioAsync(phoneNumber, message);
                    default:
                        _logger.LogWarning($"Bilinmeyen SMS sağlayıcısı: {smsProvider}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SMS gönderilirken hata oluştu: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendPasswordResetCodeAsync(string phoneNumber, string code)
        {
            var message = $"Alanya İşletme Rehberi - Şifre sıfırlama kodunuz: {code}. Bu kodu kimseyle paylaşmayın.";
            return await SendSmsAsync(phoneNumber, message);
        }

        private string CleanPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
                return string.Empty;

            // Boşluk, tire, parantez ve + işaretini kaldır
            var cleaned = phoneNumber.Replace(" ", "")
                                    .Replace("-", "")
                                    .Replace("(", "")
                                    .Replace(")", "")
                                    .Replace("+", "");

            // Türkiye için 90 ile başlamıyorsa ekle
            if (!cleaned.StartsWith("90") && cleaned.Length == 10)
            {
                cleaned = "90" + cleaned;
            }

            return cleaned;
        }

        private async Task<bool> SendViaNetgsmAsync(string phoneNumber, string message)
        {
            try
            {
                var username = _configuration["SmsSettings:Netgsm:Username"];
                var password = _configuration["SmsSettings:Netgsm:Password"];
                var apiUrl = _configuration["SmsSettings:Netgsm:ApiUrl"] ?? "https://api.netgsm.com.tr/sms/send/get";

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    _logger.LogWarning("Netgsm SMS ayarları yapılandırılmamış.");
                    return false;
                }

                // Netgsm API çağrısı
                using var httpClient = new HttpClient();
                var requestUrl = $"{apiUrl}?usercode={username}&password={password}&gsmno={phoneNumber}&message={Uri.EscapeDataString(message)}&msgheader={Uri.EscapeDataString(_configuration["SmsSettings:Netgsm:Sender"] ?? "ALANYA")}";
                
                var response = await httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode && responseContent.StartsWith("00"))
                {
                    _logger.LogInformation($"SMS başarıyla gönderildi: {phoneNumber}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"SMS gönderilemedi: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Netgsm SMS gönderilirken hata: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SendViaTwilioAsync(string phoneNumber, string message)
        {
            try
            {
                var accountSid = _configuration["SmsSettings:Twilio:AccountSid"];
                var authToken = _configuration["SmsSettings:Twilio:AuthToken"];
                var fromNumber = _configuration["SmsSettings:Twilio:FromNumber"];

                if (string.IsNullOrEmpty(accountSid) || string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(fromNumber))
                {
                    _logger.LogWarning("Twilio SMS ayarları yapılandırılmamış.");
                    return false;
                }

                // Twilio API çağrısı
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Basic", Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}")));

                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("From", fromNumber),
                    new KeyValuePair<string, string>("To", $"+{phoneNumber}"),
                    new KeyValuePair<string, string>("Body", message)
                });

                var response = await httpClient.PostAsync($"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"SMS başarıyla gönderildi: {phoneNumber}");
                    return true;
                }
                else
                {
                    _logger.LogWarning($"SMS gönderilemedi: {responseContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Twilio SMS gönderilirken hata: {ex.Message}");
                return false;
            }
        }
    }
}

