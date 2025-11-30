using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plafind.Options;

namespace Plafind.Services
{
    public class GeminiChatService : IGeminiChatService
    {
        private const string Endpoint =
            "https://generativelanguage.googleapis.com/v1/models/gemini-1.5-flash:generateContent?key=";

        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiChatService> _logger;
        private readonly GeminiOptions _options;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public GeminiChatService(
            HttpClient httpClient,
            IOptions<GeminiOptions> options,
            ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<string> AskAsync(string prompt, double? latitude = null, double? longitude = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Gemini API anahtarı yapılandırılmamış.");
            }

            var sb = new StringBuilder();
            sb.AppendLine("Aşağıdaki kullanıcı isteğine Türkçe yanıt ver.");
            sb.AppendLine("Bağlam: Alanya ve çevresindeki işletmeler, restoranlar, oteller, aktiviteler.");
            sb.AppendLine("Yanıtlarını kısa paragraflar halinde üret ve mümkünse önerileri maddelendir.");
            if (latitude.HasValue && longitude.HasValue)
            {
                sb.AppendLine("Kullanıcının yaklaşık konumu: " +
                    $"{latitude.Value.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{longitude.Value.ToString(CultureInfo.InvariantCulture)}. Yakın çevresine uygun öneriler ver.");
            }
            sb.AppendLine("Kullanıcı talebi:");
            sb.AppendLine(prompt);

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = sb.ToString()
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint + _options.ApiKey)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini API başarısız oldu: {Status} - {Body}", response.StatusCode, error);
                    throw new ApplicationException("Gemini API isteği başarısız oldu.");
                }

                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var text = document.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return string.IsNullOrWhiteSpace(text)
                    ? "Şu anda yanıt üretemiyorum, lütfen tekrar deneyin."
                    : text.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gemini isteği sırasında hata oluştu.");
                throw;
            }
        }
    }
}

