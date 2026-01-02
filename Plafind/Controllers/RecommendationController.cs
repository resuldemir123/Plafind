using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plafind.Models;
using Plafind.Services;
using Plafind.Data;

namespace Plafind.Controllers
{
    [Authorize]
    public class RecommendationController : Controller
    {
        private readonly IRecommendationService _recommendationService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationService recommendationService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewData["Title"] = "Bugün Ne Yapsam? - Kişiselleştirilmiş Öneriler";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetRecommendations(string? timeOfDay = null, string? weather = null)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
                }

                // Zaman dilimi belirlenmemişse şu anki saati kullan
                if (string.IsNullOrEmpty(timeOfDay))
                {
                    var hour = DateTime.Now.Hour;
                    if (hour >= 6 && hour < 12)
                        timeOfDay = "Sabah";
                    else if (hour >= 12 && hour < 17)
                        timeOfDay = "Öğlen";
                    else if (hour >= 17 && hour < 21)
                        timeOfDay = "Akşamüstü";
                    else
                        timeOfDay = "Akşam";
                }

                // Hava durumu belirlenmemişse varsayılan
                if (string.IsNullOrEmpty(weather))
                {
                    weather = "Güneşli";
                }

                var result = await _recommendationService.GetPersonalizedRecommendationsAsync(
                    user.Id,
                    timeOfDay,
                    weather
                );

                // İşletme detaylarını çek
                var businessIds = result.RecommendedBusinesses.Select(b => b.BusinessId).ToList();
                var businesses = await _context.Businesses
                    .Include(b => b.Category)
                    .Where(b => businessIds.Contains(b.Id))
                    .ToListAsync();

                // İşletme bilgilerini güncelle
                foreach (var recommendation in result.RecommendedBusinesses)
                {
                    var business = businesses.FirstOrDefault(b => b.Id == recommendation.BusinessId);
                    if (business != null)
                    {
                        recommendation.ImageUrl = business.ImageUrl;
                        recommendation.Rating = business.AverageRating;
                        recommendation.TotalReviews = business.TotalReviews;
                        recommendation.Category = business.Category?.Name ?? "";
                        recommendation.PriceRange = business.PriceRange;
                    }
                }

                return Json(new
                {
                    success = true,
                    recommendationText = result.RecommendationText,
                    recommendedBusinesses = result.RecommendedBusinesses,
                    reasoning = result.Reasoning
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Öneri alınırken hata oluştu");
                return Json(new { success = false, message = "Öneriler alınırken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }
    }
}

