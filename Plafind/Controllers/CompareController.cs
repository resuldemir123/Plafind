using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Plafind.Services;
using Plafind.ViewModels.Compare;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Plafind.Controllers
{
    [AllowAnonymous]
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICompareService _compareService;

        public CompareController(ApplicationDbContext context, ICompareService compareService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _compareService = compareService ?? throw new ArgumentNullException(nameof(compareService));
        }

        private string? GetCurrentUserId()
        {
            return User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // GET: Compare
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var compareIds = _compareService.GetCompareList(HttpContext.Session, userId);
            var viewModel = new CompareIndexVM();
            
            if (compareIds.Count == 0)
            {
                viewModel.EmptyMessage = "Karşılaştırma listeniz boş. İşletme detay sayfalarından işletmeleri karşılaştırmaya ekleyebilirsiniz.";
                return View(viewModel);
            }

            // DB'den işletmeleri çek
            var businesses = await _context.Businesses
                .Where(b => compareIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Include(b => b.Reviews.Where(r => r.IsActive && r.IsApproved))
                .Include(b => b.Images.Where(img => img.IsActive))
                .Include(b => b.Campaigns.Where(c => c.IsActive && c.IsApproved && c.EndDate >= DateTime.Now))
                .Include(b => b.Events.Where(e => e.IsActive && e.IsApproved && e.StartDate >= DateTime.Now))
                .ToListAsync();

            // Sıralamayı koru (session sırasına göre)
            var orderedBusinesses = compareIds
                .Select(id => businesses.FirstOrDefault(b => b.Id == id))
                .Where(b => b != null)
                .Cast<Business>()
                .ToList();

            if (!orderedBusinesses.Any())
            {
                viewModel.EmptyMessage = "Karşılaştırma listenizdeki işletmeler bulunamadı.";
                return View(viewModel);
            }

            // CompareBusinessVM listesi oluştur (Service metodlarını kullan)
            var businessVMs = orderedBusinesses.Select(b =>
            {
                var priceValue = _compareService.GetPriceRangeValue(b.PriceRange);
                return new CompareBusinessVM
                {
                    Id = b.Id,
                    Name = b.Name ?? "İsimsiz",
                    CategoryName = b.Category?.Name,
                    PriceRange = b.PriceRange,
                    AverageRating = b.AverageRating,
                    TotalReviews = b.TotalReviews,
                    ImageUrl = b.ImageUrl,
                    Address = b.Address,
                    Phone = b.Phone,
                    Email = b.Email,
                    Website = b.Website,
                    WorkingHours = b.WorkingHours,
                    IsFeatured = b.IsFeatured,
                    CreatedDate = b.CreatedDate,
                    PriceValue = priceValue,
                    ValueScore = _compareService.GetValueScore(priceValue, b.AverageRating),
                    ActiveReviewsCount = b.Reviews?.Count(r => r.IsActive && r.IsApproved) ?? 0,
                    ActiveImagesCount = b.Images?.Count(img => img.IsActive) ?? 0,
                    ActiveCampaignsCount = b.Campaigns?.Count(c => c.IsActive && c.IsApproved && c.EndDate >= DateTime.Now) ?? 0,
                    UpcomingEventsCount = b.Events?.Count(e => e.IsActive && e.IsApproved && e.StartDate >= DateTime.Now) ?? 0
                };
            }).ToList();

            // Analizi Service'e taşı
            viewModel = await _compareService.PrepareComparisonAnalysisAsync(_context, businessVMs);
            
            return View(viewModel);
        }

        // POST: Compare/Add
        [HttpPost]
        public async Task<IActionResult> Add(int businessId)
        {
            var userId = GetCurrentUserId();
            // Kategori kontrolü: Eğer listede işletme varsa, aynı kategoride olmalı
            var compareIds = _compareService.GetCompareList(HttpContext.Session, userId);
            int? existingCategoryId = null;

            if (compareIds.Any())
            {
                var existingBusiness = await _context.Businesses
                    .Where(b => compareIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                    .Select(b => b.CategoryId)
                    .FirstOrDefaultAsync();

                if (existingBusiness.HasValue)
                {
                    existingCategoryId = existingBusiness.Value;

                    // Önce işletmenin var olup olmadığını kontrol et
                    var businessExists = await _context.Businesses
                        .AnyAsync(b => b.Id == businessId && b.IsActive && b.IsApproved);
                    
                    if (!businessExists)
                    {
                        return Json(new { success = false, message = "İşletme bulunamadı veya aktif değil." });
                    }

                    var newBusinessCategory = await _context.Businesses
                        .Where(b => b.Id == businessId && b.IsActive && b.IsApproved)
                        .Select(b => b.CategoryId)
                        .FirstOrDefaultAsync();

                    // newBusinessCategory null olamaz çünkü yukarıda varlığını kontrol ettik
                    if (newBusinessCategory.HasValue && newBusinessCategory.Value != existingCategoryId)
                    {
                        return Json(new { success = false, message = "Farklı kategorideki işletmeleri karşılaştıramazsınız. Lütfen aynı kategorideki işletmeleri seçin." });
                    }
                }
            }

            var result = _compareService.AddToCompareList(HttpContext.Session, businessId, existingCategoryId, userId);
            return Json(new { success = result.success, message = result.message, count = result.count });
        }

        // POST: Compare/Remove
        [HttpPost]
        public IActionResult Remove(int businessId)
        {
            var userId = GetCurrentUserId();
            var result = _compareService.RemoveFromCompareList(HttpContext.Session, businessId, userId);
            return Json(new { success = result.success, message = result.message, count = result.count });
        }

        // POST: Compare/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            var userId = GetCurrentUserId();
            _compareService.ClearCompareList(HttpContext.Session, userId);
            return Json(new { success = true, message = "Karşılaştırma listesi temizlendi." });
        }

        // GET: Compare/Count
        [HttpGet]
        public IActionResult GetCount()
        {
            var userId = GetCurrentUserId();
            var count = _compareService.GetCompareListCount(HttpContext.Session, userId);
            return Json(new { count });
        }

        // GET: Compare/Check
        [HttpGet]
        public IActionResult Check(int businessId)
        {
            var userId = GetCurrentUserId();
            var isInCompare = _compareService.IsInCompareList(HttpContext.Session, businessId, userId);
            return Json(new { isInCompare });
        }

        // GET: Compare/GetList
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var userId = GetCurrentUserId();
            var compareIds = _compareService.GetCompareList(HttpContext.Session, userId);
            
            if (compareIds.Count == 0)
            {
                return Json(new { businesses = new List<object>() });
            }

            var businesses = await _context.Businesses
                .Where(b => compareIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Select(b => new
                {
                    id = b.Id,
                    name = b.Name ?? "İsimsiz",
                    category = b.Category != null ? b.Category.Name : null,
                    imageUrl = b.ImageUrl,
                    rating = b.AverageRating
                })
                .ToListAsync();

            // Sıralamayı koru
            var orderedBusinesses = compareIds
                .Select(id => businesses.FirstOrDefault(b => b.id == id))
                .Where(b => b != null)
                .ToList();

            return Json(new { businesses = orderedBusinesses });
        }
    }
}
