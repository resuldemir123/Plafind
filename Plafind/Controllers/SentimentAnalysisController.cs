using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Plafind.Models;
using Plafind.Services;
using Plafind.Data;
using Microsoft.EntityFrameworkCore;

namespace Plafind.Controllers
{
    [Authorize(Roles = "Admin,BusinessOwner")]
    public class SentimentAnalysisController : Controller
    {
        private readonly ISentimentAnalysisService _sentimentService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SentimentAnalysisController> _logger;

        public SentimentAnalysisController(
            ISentimentAnalysisService sentimentService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SentimentAnalysisController> logger)
        {
            _sentimentService = sentimentService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index(int? businessId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // BusinessOwner ise sadece kendi işletmelerini göster
            List<Business> businesses;
            if (User.IsInRole("BusinessOwner"))
            {
                businesses = await _context.Businesses
                    .Where(b => b.OwnerId == user.Id && b.IsActive)
                    .Include(b => b.Category)
                    .ToListAsync();
            }
            else // Admin ise tüm işletmeleri göster
            {
                businesses = await _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved)
                    .Include(b => b.Category)
                    .OrderByDescending(b => b.TotalReviews)
                    .Take(50)
                    .ToListAsync();
            }

            ViewBag.Businesses = businesses;
            ViewBag.SelectedBusinessId = businessId;
            ViewData["Title"] = "Yorum Analizi - Sentiment Analysis";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(int businessId, int maxReviews = 50)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
                }

                // İşletme sahibi kontrolü
                var business = await _context.Businesses.FindAsync(businessId);
                if (business == null)
                {
                    return Json(new { success = false, message = "İşletme bulunamadı" });
                }

                if (User.IsInRole("BusinessOwner") && business.OwnerId != user.Id)
                {
                    return Json(new { success = false, message = "Bu işletmeye erişim yetkiniz yok" });
                }

                var result = await _sentimentService.AnalyzeBusinessReviewsAsync(businessId, maxReviews);

                return Json(new
                {
                    success = true,
                    analysis = new
                    {
                        businessId = result.BusinessId,
                        businessName = result.BusinessName,
                        summary = result.Summary,
                        strengths = result.Strengths,
                        weaknesses = result.Weaknesses,
                        improvementAreas = result.ImprovementAreas,
                        overallSatisfactionScore = result.OverallSatisfactionScore,
                        categoryScores = result.CategoryScores,
                        totalReviewsAnalyzed = result.TotalReviewsAnalyzed,
                        analysisDate = result.AnalysisDate.ToString("dd.MM.yyyy HH:mm")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Yorum analizi yapılırken hata oluştu. BusinessId: {BusinessId}", businessId);
                return Json(new { success = false, message = "Analiz sırasında bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        public async Task<IActionResult> Dashboard(int businessId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var business = await _context.Businesses
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
            {
                return NotFound();
            }

            // İşletme sahibi kontrolü
            if (User.IsInRole("BusinessOwner") && business.OwnerId != user.Id)
            {
                return Forbid();
            }

            ViewBag.Business = business;
            ViewData["Title"] = $"Yorum Analizi - {business.Name}";

            return View();
        }
    }
}

