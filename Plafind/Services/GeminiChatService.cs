using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Plafind.Options;

namespace Plafind.Services
{
    public class GeminiChatService : IGeminiChatService
    {
        // Gemini API base URL
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
        private const string ListModelsUrl = "https://generativelanguage.googleapis.com/v1beta/models?key=";
        
        // Varsayılan model isimleri (fallback için)
        // API anahtarı 2.0 ve 2.5 serisi modellere erişim sağlıyor
        private static readonly string[] DefaultModelNames = new[]
        {
            "gemini-2.5-flash",      // En güncel ve performanslı model (öncelikli)
            "gemini-2.0-flash",      // 2.0 serisi (fallback)
            "gemini-2.5-pro",        // Pro model (alternatif)
            "gemini-2.0-pro",        // 2.0 Pro (son çare)
            "gemini-1.5-flash",      // Eski seri (fallback)
            "gemini-pro"             // En eski model (son çare)
        };
        
        // Cache için mevcut modeller listesi
        private static List<string>? _availableModels = null;
        private static DateTime _modelsCacheTime = DateTime.MinValue;

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

        // Mevcut modelleri API'den öğren
        private async Task<List<string>> GetAvailableModelsAsync()
        {
            // Cache kontrolü (5 dakika)
            if (_availableModels != null && DateTime.Now.Subtract(_modelsCacheTime).TotalMinutes < 5)
            {
                return _availableModels;
            }

            try
            {
                var listUrl = $"{ListModelsUrl}{_options.ApiKey}";
                _logger.LogInformation("Gemini modelleri listeleniyor: {Url}", listUrl.Replace(_options.ApiKey, "***"));
                
                var response = await _httpClient.GetAsync(listUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    using var document = JsonDocument.Parse(content);
                    
                    var models = new List<string>();
                    if (document.RootElement.TryGetProperty("models", out var modelsArray))
                    {
                        foreach (var model in modelsArray.EnumerateArray())
                        {
                            if (model.TryGetProperty("name", out var nameElement))
                            {
                                var name = nameElement.GetString();
                                if (!string.IsNullOrEmpty(name) && name.StartsWith("models/"))
                                {
                                    // "models/gemini-1.5-flash" -> "gemini-1.5-flash"
                                    var modelName = name.Substring(7); // "models/".Length
                                    
                                    // Sadece generateContent destekleyen modelleri al
                                    if (model.TryGetProperty("supportedGenerationMethods", out var methods))
                                    {
                                        var methodsList = methods.EnumerateArray()
                                            .Select(m => m.GetString())
                                            .Where(m => !string.IsNullOrEmpty(m))
                                            .ToList();
                                        
                                        if (methodsList.Contains("generateContent"))
                                        {
                                            models.Add(modelName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    
                    if (models.Any())
                    {
                        _availableModels = models;
                        _modelsCacheTime = DateTime.Now;
                        _logger.LogInformation("Gemini mevcut modeller: {Models}", string.Join(", ", models));
                        return models;
                    }
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Modeller listelenemedi: {Status} - {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Modeller listelenirken hata oluştu");
            }
            
            // Hata durumunda varsayılan modelleri kullan
            _logger.LogWarning("Varsayılan modeller kullanılıyor");
            return DefaultModelNames.ToList();
        }

        public async Task<string> AskAsync(string prompt, double? latitude = null, double? longitude = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("Gemini API anahtarı yapılandırılmamış.");
            }
            
            // Mevcut modelleri öğren
            var availableModels = await GetAvailableModelsAsync();

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
            
            // TEST MODU: Hard-code URL ile test (geçici olarak aktif)
            // Bu satırı test ettikten sonra kaldırabilirsiniz
            const bool USE_HARDCODED_URL = false; // true yaparak test edebilirsiniz
            
            if (USE_HARDCODED_URL)
            {
                // Hard-code URL ile test
                var testUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_options.ApiKey}";
                _logger.LogInformation("=== TEST MODU: Hard-code URL kullanılıyor ===");
                _logger.LogInformation("Test URL: {Url}", testUrl.Replace(_options.ApiKey, "***API_KEY***"));
                
                try
                {
                    var testRequest = new HttpRequestMessage(HttpMethod.Post, testUrl)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };
                    
                    var testResponse = await _httpClient.SendAsync(testRequest);
                    var testError = await testResponse.Content.ReadAsStringAsync();
                    
                    _logger.LogInformation("Test Response Status: {Status}", testResponse.StatusCode);
                    _logger.LogInformation("Test Response Body: {Body}", testError);
                    
                    if (testResponse.IsSuccessStatusCode)
                    {
                        using var document = JsonDocument.Parse(testError);
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
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Hard-code URL testi başarısız");
                }
            }
            
            // Mevcut modelleri sırayla dene (fallback mekanizması)
            Exception? lastException = null;
            string? lastError = null;
            
            foreach (var modelName in availableModels)
            {
                try
                {
                    // URL'i kesin ve doğru şekilde oluştur
                    // Format: https://generativelanguage.googleapis.com/v1beta/models/{MODEL_NAME}:generateContent?key={API_KEY}
                    var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_options.ApiKey}";
                    
                    // URL'i logla (API key'i maskele)
                    var maskedUrl = url.Replace(_options.ApiKey, "***API_KEY***");
                    _logger.LogInformation("=== Gemini API İsteği ===");
                    _logger.LogInformation("Model: {Model}", modelName);
                    _logger.LogInformation("URL: {Url}", maskedUrl);
                    _logger.LogInformation("Tam URL (ilk 100 karakter): {UrlPrefix}...", url.Substring(0, Math.Min(100, url.Length)));
                    
                    var request = new HttpRequestMessage(HttpMethod.Post, url)
                    {
                        Content = new StringContent(json, Encoding.UTF8, "application/json")
                    };

                    // Request headers'ı logla
                    _logger.LogDebug("Request Headers: Content-Type={ContentType}", request.Content.Headers.ContentType?.ToString());

                    var response = await _httpClient.SendAsync(request);
                    
                    // Response bilgilerini logla
                    _logger.LogInformation("Response Status: {Status}", response.StatusCode);
                    _logger.LogInformation("Response Headers: {Headers}", string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
                    
                    if (response.IsSuccessStatusCode)
                    {
                        // Başarılı! Yanıtı işle
                        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                        var text = document.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString();

                        _logger.LogInformation("Gemini API başarılı: Model={Model}", modelName);
                        
                        return string.IsNullOrWhiteSpace(text)
                            ? "Şu anda yanıt üretemiyorum, lütfen tekrar deneyin."
                            : text.Trim();
                    }
                    
                    // Başarısız, hata detaylarını kaydet
                    lastError = await response.Content.ReadAsStringAsync();
                    
                    _logger.LogError("=== Gemini API HATASI ===");
                    _logger.LogError("Model: {Model}", modelName);
                    _logger.LogError("Status Code: {Status}", response.StatusCode);
                    _logger.LogError("Hata Detayı: {Error}", lastError);
                    _logger.LogError("İstek URL'i: {Url}", maskedUrl);
                    
                    // Hata içeriğini parse etmeye çalış
                    try
                    {
                        using var errorDoc = JsonDocument.Parse(lastError);
                        if (errorDoc.RootElement.TryGetProperty("error", out var errorObj))
                        {
                            if (errorObj.TryGetProperty("message", out var message))
                            {
                                _logger.LogError("API Hata Mesajı: {Message}", message.GetString());
                            }
                            if (errorObj.TryGetProperty("status", out var status))
                            {
                                _logger.LogError("API Hata Status: {Status}", status.GetString());
                            }
                        }
                    }
                    catch
                    {
                        // JSON parse edilemezse devam et
                    }

                    // "Not found" hatası ise bir sonraki modeli dene
                    if (lastError.Contains("not found", StringComparison.OrdinalIgnoreCase) || 
                        response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("Model bulunamadı (404), bir sonraki model deneniyor: {Model}", modelName);
                        continue; // Bir sonraki modeli dene
                    }
                    
                    // Diğer hatalar için de devam et (rate limit, auth vb.)
                    continue;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Model denenirken hata oluştu: {Model}", modelName);
                    continue; // Bir sonraki modeli dene
                }
            }
            
            // Tüm modeller başarısız oldu
            _logger.LogError("Tüm Gemini modelleri başarısız oldu. Son hata: {Error}, Exception: {Exception}", 
                lastError, lastException?.Message);

            // Tüm modeller başarısız olduysa kullanıcıya bilgi ver
            return "Şu anda yapay zekâ servisine bağlanamıyorum. Lütfen daha sonra tekrar deneyin.";
        }
    }
}

