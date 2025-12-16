using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Services
{
    public interface IGoogleNewsService
    {
        Task<List<News>> GetAlanyaTourismNewsAsync(int maxItems = 20);
        Task SyncTourismNewsToDatabaseAsync(int maxItems = 20);
    }

    public class GoogleNewsService : IGoogleNewsService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GoogleNewsService> _logger;
        
        // Senkronizasyon durumu için static değişkenler
        private static DateTime _lastSyncTime = DateTime.MinValue;
        private static int _lastSyncItemCount = 0;
        private static bool _lastSyncSuccess = false;
        private static string? _lastErrorMessage = null;
        private static TimeSpan? _lastSyncDuration = null;
        private static int _totalSyncedItems = 0;

        // Turizm ile ilgili anahtar kelimeler
        private readonly string[] _tourismKeywords = new[]
        {
            "turizm", "turist", "otel", "rezervasyon", "plaj", "sezon", "gemi",
            "gazipaşa havalimanı", "havalimanı", "tatil", "konaklama", "restoran",
            "aktivite", "gezi", "seyahat", "tur", "kruvaziyer", "yacht", "marina",
            "sahil", "kumsal", "deniz", "diving", "dalış", "rafting", "safari"
        };

        public GoogleNewsService(
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<GoogleNewsService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Son senkronizasyon durumunu döndürür
        /// </summary>
        public static NewsSyncStatus GetSyncStatus()
        {
            return new NewsSyncStatus
            {
                LastSyncTime = _lastSyncTime,
                LastSyncItemCount = _lastSyncItemCount,
                IsSuccess = _lastSyncSuccess,
                LastErrorMessage = _lastErrorMessage,
                LastSyncDuration = _lastSyncDuration,
                TotalSyncedItems = _totalSyncedItems
            };
        }

        /// <summary>
        /// Google News RSS feed'inden Alanya turizm haberlerini çeker ve filtreler
        /// </summary>
        public async Task<List<News>> GetAlanyaTourismNewsAsync(int maxItems = 20)
        {
            var startTime = DateTime.Now;
            const int maxRetries = 3;
            const int retryDelayMs = 2000; // 2 saniye
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // Google News RSS feed URL'i
                    string rssUrl = "https://news.google.com/rss/search?q=Alanya&hl=tr-TR&gl=TR&ceid=TR:tr";

                    if (attempt == 1)
                    {
                        _logger.LogInformation("=== Google News API Çağrısı Başlatıldı ===");
                        _logger.LogInformation("URL: {Url}", rssUrl);
                        _logger.LogInformation("Zaman: {Time}", startTime);
                    }
                    else
                    {
                        _logger.LogInformation("=== Retry Denemesi #{Attempt} ===", attempt);
                    }

                    // RSS feed'i çek (timeout ile)
                    _logger.LogInformation("HTTP isteği gönderiliyor...");
                    
                    // Timeout ayarla (30 saniye)
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));
                    var response = await _httpClient.GetAsync(rssUrl, cts.Token);
                
                _logger.LogInformation("HTTP Yanıt Durumu: {StatusCode} {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("HTTP Hatası: {StatusCode} - {Content}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"API yanıt hatası: {response.StatusCode}");
                }

                _logger.LogInformation("HTTP yanıtı başarılı, içerik okunuyor...");
                string xmlData = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("Alınan XML veri boyutu: {Size} karakter", xmlData.Length);
                
                if (string.IsNullOrWhiteSpace(xmlData))
                {
                    _logger.LogWarning("XML verisi boş!");
                    return new List<News>();
                }

                _logger.LogInformation("XML parse ediliyor...");
                XDocument doc = XDocument.Parse(xmlData);
                
                var totalItems = doc.Descendants("item").Count();
                _logger.LogInformation("Toplam {Count} haber öğesi bulundu", totalItems);

                // RSS feed'den haberleri parse et ve turizm anahtar kelimelerine göre filtrele
                var tourismNews = doc.Descendants("item")
                    .Select(item => new
                    {
                        Title = item.Element("title")?.Value?.Trim() ?? string.Empty,
                        Link = item.Element("link")?.Value?.Trim() ?? string.Empty,
                        Description = CleanHtmlDescription(item.Element("description")?.Value?.Trim() ?? string.Empty),
                        PubDate = ParsePubDate(item.Element("pubDate")?.Value),
                        ImageUrl = ExtractImageUrl(item.Element("description")?.Value)
                    })
                    .Where(news => !string.IsNullOrEmpty(news.Title) && 
                                   !string.IsNullOrEmpty(news.Link) &&
                                   // Başlıkta veya açıklamada turizm anahtar kelimelerinden biri geçiyor mu?
                                   _tourismKeywords.Any(keyword => 
                                       news.Title.ToLowerInvariant().Contains(keyword.ToLowerInvariant()) ||
                                       news.Description.ToLowerInvariant().Contains(keyword.ToLowerInvariant())))
                    .Take(maxItems)
                    .Select(news => new News
                    {
                        Title = System.Net.WebUtility.HtmlDecode(news.Title),
                        Content = !string.IsNullOrEmpty(news.Description) 
                            ? $"<div class=\"news-external-content\"><p>{System.Net.WebUtility.HtmlEncode(news.Description)}</p><div class=\"mt-4\"><a href=\"{System.Net.WebUtility.HtmlEncode(news.Link)}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"btn btn-primary btn-lg\"><i class=\"fas fa-external-link-alt me-2\"></i>Haberi Okuyun</a></div></div>"
                            : $"<div class=\"news-external-content\"><div class=\"mt-3\"><a href=\"{System.Net.WebUtility.HtmlEncode(news.Link)}\" target=\"_blank\" rel=\"noopener noreferrer\" class=\"btn btn-primary btn-lg\"><i class=\"fas fa-external-link-alt me-2\"></i>Haberi Okuyun</a></div></div>",
                        SourceUrl = news.Link,
                        ImageUrl = news.ImageUrl,
                        PublishDate = news.PubDate,
                        ViewCount = 0,
                        IsExternal = true,
                        ExternalSource = "Google News"
                    })
                    .ToList();

                    var duration = DateTime.Now - startTime;
                    _logger.LogInformation("=== API Çağrısı Başarılı ===");
                    _logger.LogInformation("Bulunan turizm haberi sayısı: {Count}", tourismNews.Count);
                    _logger.LogInformation("İşlem süresi: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogInformation("Deneme sayısı: {Attempt}", attempt);
                    _logger.LogInformation("===========================");

                    return tourismNews;
                }
                catch (HttpRequestException ex)
                {
                    var duration = DateTime.Now - startTime;
                    
                    // DNS hatası veya ağ hatası kontrolü
                    bool isNetworkError = ex.Message.Contains("Bilinen böyle bir ana bilgisayar yok") ||
                                         ex.Message.Contains("No such host") ||
                                         ex.Message.Contains("Name or service not known") ||
                                         ex.InnerException is System.Net.Sockets.SocketException;
                    
                    if (isNetworkError && attempt < maxRetries)
                    {
                        _logger.LogWarning("=== Ağ/DNS Hatası - Retry Yapılıyor ===");
                        _logger.LogWarning("Hata: {Message}", ex.Message);
                        _logger.LogWarning("Deneme: {Attempt}/{MaxRetries}", attempt, maxRetries);
                        _logger.LogWarning("{Delay}ms sonra tekrar denenecek...", retryDelayMs);
                        _logger.LogWarning("=====================================");
                        
                        await Task.Delay(retryDelayMs * attempt); // Exponential backoff
                        continue; // Retry
                    }
                    
                    _logger.LogError(ex, "=== API Çağrısı HTTP Hatası ===");
                    _logger.LogError("Hata: {Message}", ex.Message);
                    _logger.LogError("İşlem süresi: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogError("Deneme sayısı: {Attempt}", attempt);
                    
                    if (isNetworkError)
                    {
                        _logger.LogError("DNS/Ağ bağlantı hatası. Lütfen internet bağlantınızı kontrol edin.");
                    }
                    
                    _logger.LogError("================================");
                    
                    if (attempt == maxRetries)
                    {
                        return new List<News>();
                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException ex)
                {
                    var duration = DateTime.Now - startTime;
                    
                    if (attempt < maxRetries)
                    {
                        _logger.LogWarning("=== Timeout Hatası - Retry Yapılıyor ===");
                        _logger.LogWarning("Hata: {Message}", ex.Message);
                        _logger.LogWarning("Deneme: {Attempt}/{MaxRetries}", attempt, maxRetries);
                        _logger.LogWarning("{Delay}ms sonra tekrar denenecek...", retryDelayMs);
                        _logger.LogWarning("=====================================");
                        
                        await Task.Delay(retryDelayMs * attempt);
                        continue; // Retry
                    }
                    
                    _logger.LogError(ex, "=== API Çağrısı Timeout Hatası ===");
                    _logger.LogError("Hata: {Message}", ex.Message);
                    _logger.LogError("İşlem süresi: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogError("Deneme sayısı: {Attempt}", attempt);
                    _logger.LogError("================================");
                    
                    if (attempt == maxRetries)
                    {
                        return new List<News>();
                    }
                }
                catch (Exception ex)
                {
                    var duration = DateTime.Now - startTime;
                    
                    if (attempt < maxRetries && ex.Message.Contains("network") || ex.Message.Contains("DNS"))
                    {
                        _logger.LogWarning("=== Genel Ağ Hatası - Retry Yapılıyor ===");
                        _logger.LogWarning("Hata: {Message}", ex.Message);
                        _logger.LogWarning("Deneme: {Attempt}/{MaxRetries}", attempt, maxRetries);
                        _logger.LogWarning("{Delay}ms sonra tekrar denenecek...", retryDelayMs);
                        _logger.LogWarning("=====================================");
                        
                        await Task.Delay(retryDelayMs * attempt);
                        continue; // Retry
                    }
                    
                    _logger.LogError(ex, "=== Google News API Çağrısı Genel Hata ===");
                    _logger.LogError("Hata: {Message}", ex.Message);
                    _logger.LogError("İşlem süresi: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogError("Deneme sayısı: {Attempt}", attempt);
                    _logger.LogError("===========================================");
                    
                    if (attempt == maxRetries)
                    {
                        return new List<News>();
                    }
                }
            }
            
            // Tüm denemeler başarısız oldu
            var finalDuration = DateTime.Now - startTime;
            _logger.LogError("=== Tüm Denemeler Başarısız ===");
            _logger.LogError("Toplam deneme sayısı: {MaxRetries}", maxRetries);
            _logger.LogError("Toplam süre: {Duration}ms", finalDuration.TotalMilliseconds);
            _logger.LogError("=============================");
            
            return new List<News>();
        }

        /// <summary>
        /// Turizm haberlerini veritabanına senkronize eder (duplicate kontrolü ile)
        /// </summary>
        public async Task SyncTourismNewsToDatabaseAsync(int maxItems = 20)
        {
            var syncStartTime = DateTime.Now;
            _lastSyncSuccess = false;
            _lastErrorMessage = null;
            
            try
            {
                _logger.LogInformation("=== Haber Senkronizasyonu Başlatıldı ===");
                _logger.LogInformation("Zaman: {Time}", syncStartTime);
                _logger.LogInformation("Maksimum haber sayısı: {MaxItems}", maxItems);
                
                var newsItems = await GetAlanyaTourismNewsAsync(maxItems);
                
                _logger.LogInformation("API'den {Count} haber alındı", newsItems.Count);

                if (!newsItems.Any())
                {
                    var duration = DateTime.Now - syncStartTime;
                    _lastSyncTime = DateTime.Now;
                    _lastSyncItemCount = 0;
                    _lastSyncSuccess = true;
                    _lastSyncDuration = duration;
                    _logger.LogWarning("Senkronize edilecek haber bulunamadı");
                    _logger.LogInformation("=== Senkronizasyon Tamamlandı (Haber Yok) ===");
                    _logger.LogInformation("Süre: {Duration}ms", duration.TotalMilliseconds);
                    return;
                }

                int addedCount = 0;
                int skippedCount = 0;

                foreach (var newsItem in newsItems)
                {
                    // Aynı SourceUrl'e sahip haber zaten var mı kontrol et
                    var existingNews = await _context.News
                        .FirstOrDefaultAsync(n => n.SourceUrl == newsItem.SourceUrl && n.IsExternal);

                    if (existingNews != null)
                    {
                        skippedCount++;
                        _logger.LogDebug("Haber zaten mevcut, atlandı: {Title}", newsItem.Title);
                        continue;
                    }

                    // Yeni haber ekle
                    _context.News.Add(newsItem);
                    addedCount++;
                }

                if (addedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _totalSyncedItems += addedCount;
                    var duration = DateTime.Now - syncStartTime;
                    
                    _lastSyncTime = DateTime.Now;
                    _lastSyncItemCount = addedCount;
                    _lastSyncSuccess = true;
                    _lastSyncDuration = duration;
                    
                    _logger.LogInformation("=== Senkronizasyon Başarılı ===");
                    _logger.LogInformation("Yeni eklenen: {AddedCount}", addedCount);
                    _logger.LogInformation("Atlanan: {SkippedCount}", skippedCount);
                    _logger.LogInformation("Toplam senkronize edilen: {Total}", _totalSyncedItems);
                    _logger.LogInformation("Süre: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogInformation("=============================");
                }
                else
                {
                    var duration = DateTime.Now - syncStartTime;
                    
                    _lastSyncTime = DateTime.Now;
                    _lastSyncItemCount = 0;
                    _lastSyncSuccess = true;
                    _lastSyncDuration = duration;
                    
                    _logger.LogInformation("=== Senkronizasyon Tamamlandı (Yeni Haber Yok) ===");
                    _logger.LogInformation("Atlanan: {SkippedCount} haber", skippedCount);
                    _logger.LogInformation("Süre: {Duration}ms", duration.TotalMilliseconds);
                    _logger.LogInformation("================================================");
                }
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - syncStartTime;
                
                _lastSyncTime = DateTime.Now;
                _lastSyncItemCount = 0;
                _lastSyncSuccess = false;
                _lastErrorMessage = ex.Message;
                _lastSyncDuration = duration;
                
                _logger.LogError(ex, "=== Senkronizasyon Hatası ===");
                _logger.LogError("Hata: {Message}", ex.Message);
                _logger.LogError("Süre: {Duration}ms", duration.TotalMilliseconds);
                _logger.LogError("==============================");
                throw;
            }
        }

        /// <summary>
        /// RSS pubDate string'ini DateTime'a çevirir
        /// </summary>
        private DateTime ParsePubDate(string? pubDateString)
        {
            if (string.IsNullOrEmpty(pubDateString))
                return DateTime.Now;

            // RSS pubDate formatı genellikle: "Mon, 01 Jan 2024 12:00:00 GMT"
            if (DateTime.TryParse(pubDateString, out DateTime parsedDate))
                return parsedDate;

            return DateTime.Now;
        }

        /// <summary>
        /// RSS description'dan HTML etiketlerini temizler ve düz metin döndürür
        /// </summary>
        private string CleanHtmlDescription(string description)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            // HTML etiketlerini kaldır
            var cleaned = System.Text.RegularExpressions.Regex.Replace(description, "<.*?>", string.Empty);
            
            // HTML entity'lerini decode et
            cleaned = System.Net.WebUtility.HtmlDecode(cleaned);
            
            // Fazla boşlukları temizle
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();
            
            return cleaned;
        }

        /// <summary>
        /// RSS description'dan görsel URL'sini çıkarır
        /// </summary>
        private string? ExtractImageUrl(string? description)
        {
            if (string.IsNullOrEmpty(description))
                return null;

            // HTML içindeki img tag'inden src attribute'unu çıkar
            var startIndex = description.IndexOf("<img", StringComparison.OrdinalIgnoreCase);
            if (startIndex == -1)
                return null;

            var srcStart = description.IndexOf("src=\"", startIndex, StringComparison.OrdinalIgnoreCase);
            if (srcStart == -1)
                return null;

            srcStart += 5; // "src=\"" uzunluğu
            var srcEnd = description.IndexOf("\"", srcStart);
            if (srcEnd == -1)
                return null;

            return description.Substring(srcStart, srcEnd - srcStart);
        }
    }
}

