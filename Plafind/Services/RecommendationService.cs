using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using Plafind.Models;
using System.Text;

namespace Plafind.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeminiChatService _geminiService;
        private readonly ILogger<RecommendationService> _logger;

        public RecommendationService(
            ApplicationDbContext context,
            IGeminiChatService geminiService,
            ILogger<RecommendationService> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<RecommendationResult> GetPersonalizedRecommendationsAsync(
            string userId, 
            string? timeOfDay = null, 
            string? weather = null)
        {
            try
            {
                // Kullanıcının favori işletmelerini çek
                var favorites = await _context.UserFavorites
                    .Include(f => f.Business)
                        .ThenInclude(b => b.Category)
                    .Where(f => f.UserId == userId && f.Business != null)
                    .Select(f => f.Business!)
                    .ToListAsync();

                // Kullanıcının geçmiş rezervasyonlarını çek
                var reservations = await _context.Reservations
                    .Include(r => r.Business)
                        .ThenInclude(b => b.Category)
                    .Where(r => r.UserId == userId && r.Business != null)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .ToListAsync();

                // En çok tıklanan kategorileri belirle (rezervasyon ve favorilerden)
                var categoryPreferences = favorites
                    .Select(f => f.Category?.Name)
                    .Concat(reservations.Select(r => r.Business?.Category?.Name))
                    .Where(c => !string.IsNullOrEmpty(c))
                    .GroupBy(c => c)
                    .OrderByDescending(g => g.Count())
                    .Take(5)
                    .Select(g => g.Key)
                    .ToList();

                // Veritabanındaki tüm aktif işletmeleri çek (öneri için)
                var availableBusinesses = await _context.Businesses
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .Where(b => b.IsActive && b.IsApproved)
                    .Take(50) // Performans için limit
                    .ToListAsync();

                // Gemini'ye gönderilecek prompt'u oluştur
                var prompt = BuildRecommendationPrompt(
                    favorites,
                    reservations,
                    categoryPreferences,
                    availableBusinesses,
                    timeOfDay ?? DateTime.Now.ToString("HH:mm"),
                    weather ?? "Güneşli"
                );

                // Gemini'den öneri al
                var geminiResponse = await _geminiService.AskAsync(prompt);

                // Gemini yanıtını parse et ve işletme ID'lerini çıkar
                var recommendedBusinesses = ParseGeminiResponse(geminiResponse, availableBusinesses);

                return new RecommendationResult
                {
                    RecommendationText = geminiResponse,
                    RecommendedBusinesses = recommendedBusinesses,
                    Reasoning = ExtractReasoning(geminiResponse)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kişiselleştirilmiş öneri oluşturulurken hata oluştu");
                
                // Fallback: En popüler işletmeleri öner
                var fallbackBusinesses = await _context.Businesses
                    .Include(b => b.Category)
                    .Where(b => b.IsActive && b.IsApproved)
                    .OrderByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.TotalReviews)
                    .Take(5)
                    .ToListAsync();

                return new RecommendationResult
                {
                    RecommendationText = "Size özel öneriler hazırlanırken bir sorun oluştu. En popüler işletmeleri gösteriyoruz.",
                    RecommendedBusinesses = fallbackBusinesses.Select(b => new BusinessRecommendation
                    {
                        BusinessId = b.Id,
                        BusinessName = b.Name ?? "",
                        Category = b.Category?.Name ?? "",
                        Reason = "Popüler işletme",
                        Rating = b.AverageRating,
                        TotalReviews = b.TotalReviews,
                        ImageUrl = b.ImageUrl,
                        PriceRange = b.PriceRange
                    }).ToList(),
                    Reasoning = "Sistem hatası nedeniyle genel öneriler gösteriliyor."
                };
            }
        }

        private string BuildRecommendationPrompt(
            List<Business> favorites,
            List<Reservation> reservations,
            List<string?> categoryPreferences,
            List<Business> availableBusinesses,
            string timeOfDay,
            string weather)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Sen Alanya yerel rehberisin. Aşağıdaki bilgilere göre kullanıcıya kişiselleştirilmiş işletme önerileri sun.");
            sb.AppendLine();
            
            // Favoriler
            if (favorites.Any())
            {
                sb.AppendLine("Kullanıcının favori işletmeleri:");
                foreach (var fav in favorites.Where(f => f != null).Take(10))
                {
                    sb.AppendLine($"- {fav.Name ?? "İsimsiz"} ({fav.Category?.Name ?? "Kategori yok"}) - Puan: {fav.AverageRating:F1}");
                }
            }
            else
            {
                sb.AppendLine("Kullanıcının henüz favori işletmesi yok.");
            }
            
            sb.AppendLine();
            
            // Rezervasyon geçmişi
            if (reservations.Any())
            {
                sb.AppendLine("Kullanıcının geçmiş rezervasyonları:");
                foreach (var res in reservations.Take(5))
                {
                    if (res.Business != null)
                    {
                        sb.AppendLine($"- {res.Business.Name ?? "İsimsiz"} ({res.Business.Category?.Name ?? "Kategori yok"}) - Tarih: {res.RequestedDate:dd.MM.yyyy}");
                    }
                }
            }
            
            sb.AppendLine();
            
            // Kategori tercihleri
            if (categoryPreferences.Any())
            {
                sb.AppendLine($"Kullanıcının tercih ettiği kategoriler: {string.Join(", ", categoryPreferences)}");
            }
            
            sb.AppendLine();
            
            // Zaman ve hava durumu
            sb.AppendLine($"Şu anki durum: Saat {timeOfDay}, Hava durumu: {weather}");
            sb.AppendLine();
            
            // Mevcut işletmeler
            sb.AppendLine("Veritabanımızdaki işletmeler:");
            foreach (var business in availableBusinesses.Where(b => b != null).Take(30))
            {
                var features = !string.IsNullOrEmpty(business.FeaturesJson) 
                    ? " (Özellikler mevcut)" 
                    : "";
                sb.AppendLine($"- {business.Name ?? "İsimsiz"} ({business.Category?.Name ?? "Kategori yok"}) - Puan: {business.AverageRating:F1} - {business.TotalReviews} yorum - Fiyat: {business.PriceRange ?? "Belirtilmemiş"}{features}");
                if (!string.IsNullOrEmpty(business.Description))
                {
                    sb.AppendLine($"  Açıklama: {business.Description.Substring(0, Math.Min(100, business.Description.Length))}");
                }
            }
            
            sb.AppendLine();
            sb.AppendLine("Yukarıdaki bilgilere göre:");
            sb.AppendLine("1. Kullanıcıya bugün için 3-5 işletme öner (işletme adlarını tam olarak yaz)");
            sb.AppendLine("2. Her öneri için kısa bir neden belirt");
            sb.AppendLine("3. Önerileri zaman dilimine göre sırala (öğlen, akşamüstü, akşam)");
            sb.AppendLine("4. Genel bir günlük rota önerisi sun");
            sb.AppendLine();
            sb.AppendLine("Yanıtını Türkçe, samimi ve kişisel bir dille ver. İşletme adlarını tam olarak kullan.");
            
            return sb.ToString();
        }

        private List<BusinessRecommendation> ParseGeminiResponse(string response, List<Business> availableBusinesses)
        {
            var recommendations = new List<BusinessRecommendation>();
            
            // Gemini yanıtından işletme adlarını bul ve eşleştir
            foreach (var business in availableBusinesses)
            {
                if (business.Name != null && response.Contains(business.Name, StringComparison.OrdinalIgnoreCase))
                {
                    // İşletme adının etrafındaki metni al (neden için)
                    var index = response.IndexOf(business.Name, StringComparison.OrdinalIgnoreCase);
                    var start = Math.Max(0, index - 100);
                    var end = Math.Min(response.Length, index + business.Name.Length + 200);
                    var context = response.Substring(start, end - start);
                    
                    recommendations.Add(new BusinessRecommendation
                    {
                        BusinessId = business.Id,
                        BusinessName = business.Name,
                        Category = business.Category?.Name ?? "",
                        Reason = ExtractReasonFromContext(context, business.Name),
                        Rating = business.AverageRating,
                        TotalReviews = business.TotalReviews,
                        ImageUrl = business.ImageUrl,
                        PriceRange = business.PriceRange
                    });
                }
            }
            
            // Eğer hiç eşleşme yoksa, en popüler 5 işletmeyi öner
            if (!recommendations.Any())
            {
                recommendations = availableBusinesses
                    .OrderByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.TotalReviews)
                    .Take(5)
                    .Select(b => new BusinessRecommendation
                    {
                        BusinessId = b.Id,
                        BusinessName = b.Name ?? "",
                        Category = b.Category?.Name ?? "",
                        Reason = "Popüler ve yüksek puanlı işletme",
                        Rating = b.AverageRating,
                        TotalReviews = b.TotalReviews,
                        ImageUrl = b.ImageUrl,
                        PriceRange = b.PriceRange
                    })
                    .ToList();
            }
            
            return recommendations.DistinctBy(r => r.BusinessId).ToList();
        }

        private string ExtractReasonFromContext(string context, string businessName)
        {
            // Basit bir neden çıkarma (daha gelişmiş NLP yapılabilir)
            var sentences = context.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sentence in sentences)
            {
                if (sentence.Contains(businessName, StringComparison.OrdinalIgnoreCase) && sentence.Length > 20)
                {
                    return sentence.Trim();
                }
            }
            return "Size uygun bir seçenek";
        }

        private string ExtractReasoning(string response)
        {
            // İlk 200 karakteri reasoning olarak al
            return response.Length > 200 
                ? response.Substring(0, 200) + "..." 
                : response;
        }
    }
}

