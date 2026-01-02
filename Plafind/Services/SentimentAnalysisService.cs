using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using Plafind.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Plafind.Services
{
    public class SentimentAnalysisService : ISentimentAnalysisService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeminiChatService _geminiService;
        private readonly ILogger<SentimentAnalysisService> _logger;

        public SentimentAnalysisService(
            ApplicationDbContext context,
            IGeminiChatService geminiService,
            ILogger<SentimentAnalysisService> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<SentimentAnalysisResult> AnalyzeBusinessReviewsAsync(int businessId, int maxReviews = 50)
        {
            try
            {
                // İşletmeyi çek
                var business = await _context.Businesses
                    .Include(b => b.Category)
                    .FirstOrDefaultAsync(b => b.Id == businessId);

                if (business == null)
                {
                    throw new ArgumentException($"İşletme bulunamadı: {businessId}");
                }

                // Son yorumları çek
                var reviews = await _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.BusinessId == businessId && r.IsActive && r.IsApproved && !string.IsNullOrEmpty(r.Comment))
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(maxReviews)
                    .ToListAsync();

                if (!reviews.Any())
                {
                    return new SentimentAnalysisResult
                    {
                        BusinessId = businessId,
                        BusinessName = business.Name ?? "",
                        Summary = "Bu işletme için henüz yorum bulunmamaktadır.",
                        OverallSatisfactionScore = 0,
                        TotalReviewsAnalyzed = 0
                    };
                }

                // Gemini'ye gönderilecek prompt'u oluştur
                var prompt = BuildAnalysisPrompt(business, reviews);

                // Gemini'den analiz al
                var geminiResponse = await _geminiService.AskAsync(prompt);

                // Gemini yanıtını parse et
                var result = ParseGeminiResponse(geminiResponse, business, reviews.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum analizi yapılırken hata oluştu. BusinessId: {BusinessId}", businessId);
                
                // Fallback: Basit istatistiksel analiz
                var reviews = await _context.Reviews
                    .Where(r => r.BusinessId == businessId && r.IsActive && r.IsApproved)
                    .ToListAsync();

                var business = await _context.Businesses.FindAsync(businessId);

                return new SentimentAnalysisResult
                {
                    BusinessId = businessId,
                    BusinessName = business?.Name ?? "",
                    Summary = "Analiz sırasında bir hata oluştu. Basit istatistikler gösteriliyor.",
                    OverallSatisfactionScore = reviews.Any() ? reviews.Average(r => r.Rating) * 2 : 0, // 1-5 -> 0-10
                    TotalReviewsAnalyzed = reviews.Count,
                    Strengths = new List<string> { "Yorum sayısı: " + reviews.Count },
                    Weaknesses = new List<string>(),
                    ImprovementAreas = new List<string>()
                };
            }
        }

        private string BuildAnalysisPrompt(Business business, List<Review> reviews)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Aşağıdaki yorumları analiz et.");
            sb.AppendLine();
            sb.AppendLine("Şunları yap:");
            sb.AppendLine("- Genel müşteri memnuniyetini özetle");
            sb.AppendLine("- En çok memnun olunan konuları söyle");
            sb.AppendLine("- En çok şikayet edilen konuları söyle");
            sb.AppendLine("- Sonuçları sade bir dille işletme sahibine anlat");
            sb.AppendLine();
            sb.AppendLine("Yorumlar:");
            
            foreach (var review in reviews)
            {
                sb.AppendLine($"- \"{review.Comment}\"");
            }
            
            sb.AppendLine();
            sb.AppendLine("Yukarıdaki yorumları analiz ederek şu formatta bir rapor hazırla:");
            sb.AppendLine();
            sb.AppendLine("1. ÖZET: Genel müşteri memnuniyet durumunu 2-3 cümleyle özetle");
            sb.AppendLine();
            sb.AppendLine("2. GÜÇLÜ YANLAR: İşletmenin müşteriler tarafından beğenilen yönlerini maddeler halinde listele (en az 3, en fazla 7 madde)");
            sb.AppendLine("   Format: - [Madde açıklaması]");
            sb.AppendLine();
            sb.AppendLine("3. ZAYIF YANLAR: İşletmenin müşteriler tarafından eleştirilen yönlerini maddeler halinde listele (en az 2, en fazla 5 madde)");
            sb.AppendLine("   Format: - [Madde açıklaması]");
            sb.AppendLine();
            sb.AppendLine("4. İYİLEŞTİRME ALANLARI: Acil iyileştirilmesi gereken noktaları maddeler halinde listele (en az 2, en fazla 5 madde)");
            sb.AppendLine("   Format: - [Madde açıklaması]");
            sb.AppendLine();
            sb.AppendLine("5. GENEL MEMNUNİYET PUANI: 10 üzerinden bir puan ver (sadece sayı, örn: 7.5)");
            sb.AppendLine();
            sb.AppendLine("6. KATEGORİ BAZLI PUANLAR: Şu kategorilerde puan ver (her biri 0-10 arası):");
            sb.AppendLine("   - Servis Kalitesi: [puan]");
            sb.AppendLine("   - Lezzet/Kalite: [puan]");
            sb.AppendLine("   - Fiyat/Değer: [puan]");
            sb.AppendLine("   - Ortam/Atmosfer: [puan]");
            sb.AppendLine("   - Lokasyon/Erişim: [puan]");
            sb.AppendLine();
            sb.AppendLine("Raporunu Türkçe, sade ve anlaşılır bir dille hazırla. İşletme sahibi yorum okumak zorunda kalmadan direkt aksiyon alabilsin.");
            
            return sb.ToString();
        }

        private SentimentAnalysisResult ParseGeminiResponse(string response, Business business, int reviewCount)
        {
            var result = new SentimentAnalysisResult
            {
                BusinessId = business.Id,
                BusinessName = business.Name ?? "",
                TotalReviewsAnalyzed = reviewCount,
                AnalysisDate = DateTime.Now
            };

            // Özet çıkar
            var summaryMatch = Regex.Match(response, @"(?i)(?:özet|summary)[\s:]*([^\n]+(?:\n[^\n]+){0,2})", RegexOptions.Multiline);
            if (summaryMatch.Success)
            {
                result.Summary = summaryMatch.Groups[1].Value.Trim();
            }
            else
            {
                // İlk paragrafı özet olarak al
                var firstParagraph = response.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                result.Summary = firstParagraph ?? "Analiz tamamlandı.";
            }

            // Güçlü yanlar
            var strengthsMatch = Regex.Match(response, @"(?i)(?:güçlü\s+yanlar|strengths)[\s:]*([^-]+?)(?=zayıf|iyileştirme|genel|kategori|$)", RegexOptions.Singleline);
            if (strengthsMatch.Success)
            {
                result.Strengths = ExtractListItems(strengthsMatch.Groups[1].Value);
            }

            // Zayıf yanlar
            var weaknessesMatch = Regex.Match(response, @"(?i)(?:zayıf\s+yanlar|weaknesses)[\s:]*([^-]+?)(?=iyileştirme|genel|kategori|$)", RegexOptions.Singleline);
            if (weaknessesMatch.Success)
            {
                result.Weaknesses = ExtractListItems(weaknessesMatch.Groups[1].Value);
            }

            // İyileştirme alanları
            var improvementMatch = Regex.Match(response, @"(?i)(?:iyileştirme\s+alanları|improvement)[\s:]*([^-]+?)(?=genel|kategori|$)", RegexOptions.Singleline);
            if (improvementMatch.Success)
            {
                result.ImprovementAreas = ExtractListItems(improvementMatch.Groups[1].Value);
            }

            // Genel memnuniyet puanı
            var scoreMatch = Regex.Match(response, @"(?i)(?:genel\s+memnuniyet\s+puanı|overall\s+satisfaction)[\s:]*(\d+\.?\d*)", RegexOptions.Multiline);
            if (scoreMatch.Success && double.TryParse(scoreMatch.Groups[1].Value, out double score))
            {
                result.OverallSatisfactionScore = Math.Min(10, Math.Max(0, score));
            }
            else
            {
                // Ortalama rating'den hesapla
                result.OverallSatisfactionScore = business.AverageRating * 2; // 1-5 -> 0-10
            }

            // Kategori bazlı puanlar
            var categoryPatterns = new Dictionary<string, string>
            {
                { "Servis Kalitesi", @"servis\s+kalitesi[\s:]*(\d+\.?\d*)" },
                { "Lezzet/Kalite", @"lezzet[/\s]*kalite[\s:]*(\d+\.?\d*)" },
                { "Fiyat/Değer", @"fiyat[/\s]*değer[\s:]*(\d+\.?\d*)" },
                { "Ortam/Atmosfer", @"ortam[/\s]*atmosfer[\s:]*(\d+\.?\d*)" },
                { "Lokasyon/Erişim", @"lokasyon[/\s]*erişim[\s:]*(\d+\.?\d*)" }
            };

            foreach (var pattern in categoryPatterns)
            {
                var match = Regex.Match(response, pattern.Value, RegexOptions.IgnoreCase);
                if (match.Success && double.TryParse(match.Groups[1].Value, out double categoryScore))
                {
                    result.CategoryScores[pattern.Key] = Math.Min(10, Math.Max(0, categoryScore));
                }
            }

            return result;
        }

        private List<string> ExtractListItems(string text)
        {
            var items = new List<string>();
            var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("-") || trimmed.StartsWith("•") || trimmed.StartsWith("*"))
                {
                    var item = trimmed.Substring(1).Trim();
                    if (!string.IsNullOrEmpty(item) && item.Length > 5)
                    {
                        items.Add(item);
                    }
                }
                else if (Regex.IsMatch(trimmed, @"^\d+[\.\)]\s+"))
                {
                    var item = Regex.Replace(trimmed, @"^\d+[\.\)]\s+", "").Trim();
                    if (!string.IsNullOrEmpty(item) && item.Length > 5)
                    {
                        items.Add(item);
                    }
                }
            }
            
            return items;
        }
    }
}

