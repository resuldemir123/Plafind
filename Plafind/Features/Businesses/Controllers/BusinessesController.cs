using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Plafind.Features.Businesses.Services;
using Plafind.Features.Businesses.ViewModels;
using Plafind.Features.Businesses.Mappings;
using Plafind.Options;
using Plafind.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Security.Claims;

namespace Plafind.Features.Businesses.Controllers
{
    public class BusinessesController : Controller
    {
        private readonly IBusinessService _businessService;
        private readonly GoogleMapsOptions _mapsOptions;
        private readonly TomTomOptions _tomTomOptions;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;
        private readonly ApplicationDbContext _context;

        public BusinessesController(
            IBusinessService businessService,
            IOptions<GoogleMapsOptions> mapsOptions,
            IOptions<TomTomOptions> tomTomOptions,
            IConfiguration configuration,
            IMapper mapper,
            ApplicationDbContext context)
        {
            _businessService = businessService;
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
            _tomTomOptions = tomTomOptions?.Value ?? new TomTomOptions();
            _configuration = configuration;
            _mapper = mapper;
            _context = context;
        }

        // GET: Businesses (Giriş gerekli)
        [Authorize]
        public async Task<IActionResult> Index(
            string? search, 
            int? categoryId, 
            double? minRating, 
            double? maxRating,
            string? priceRange,
            bool? nearMe,
            double? userLatitude,
            double? userLongitude,
            List<string>? features,
            string? sortBy,
            int page = 1,
            int pageSize = 12)
        {
            var filters = new ViewModels.BusinessListViewModel
            {
                SearchQuery = search,
                CategoryId = categoryId,
                MinRating = minRating,
                MaxRating = maxRating,
                PriceRange = priceRange,
                NearMe = nearMe,
                UserLatitude = userLatitude,
                UserLongitude = userLongitude,
                Features = features ?? new List<string>(),
                SortBy = sortBy ?? "featured",
                Page = page,
                PageSize = pageSize
            };

            var result = await _businessService.GetBusinessesWithFiltersAsync(filters);
            return View(result);
        }

        // GET: Businesses/Details/5 (Giriş gerekli)
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var viewModel = await _businessService.GetBusinessDetailsAsync(id.Value);
            if (viewModel == null)
            {
                return NotFound();
            }

            var siteUrl = _configuration["SiteSettings:SiteUrl"] ?? "https://plafind.com";
            var siteName = _configuration["SiteSettings:SiteName"] ?? "Plafind";
            var business = viewModel.Business;

            // Open Graph bilgilerini set et
            ViewData["SiteUrl"] = siteUrl;
            ViewData["SiteName"] = siteName;
            ViewData["OgTitle"] = $"{business.Name} - {business.Category?.Name ?? "İşletme"} | {siteName}";
            ViewData["OgDescription"] = !string.IsNullOrWhiteSpace(business.Description)
                ? (business.Description.Length > 200 ? business.Description.Substring(0, 200) + "..." : business.Description)
                : $"{business.Name} hakkında detaylı bilgi. {siteName} üzerinden rezervasyon yapabilir, yorum okuyabilir ve değerlendirme yapabilirsiniz.";
            ViewData["OgImage"] = !string.IsNullOrWhiteSpace(business.ImageUrl)
                ? (business.ImageUrl.StartsWith("http") ? business.ImageUrl : $"{siteUrl}{business.ImageUrl}")
                : $"{siteUrl}/images/Logo.png";
            ViewData["OgUrl"] = $"{siteUrl}/Businesses/Details/{business.Id}";
            ViewData["OgType"] = "business.business";

            // Gerçek yorum sayısını hesapla (IsActive && IsApproved olanlar)
            var activeApprovedReviewsCount = business.Reviews?.Count(r => r.IsActive && r.IsApproved) ?? 0;
            ViewBag.ActiveApprovedReviewsCount = activeApprovedReviewsCount;

            // Aktif ve müşterilere görünür kampanyaları yükle
            var now = DateTime.Now;
            var activeCampaigns = await _context.Campaigns
                .Where(c => c.BusinessId == business.Id 
                    && c.IsActive 
                    && c.IsVisibleToCustomers // Müşterilere görünür olmalı
                    && c.StartDate <= now 
                    && c.EndDate >= now)
                .OrderByDescending(c => c.IsFeatured)
                .ThenByDescending(c => c.CreatedDate)
                .ToListAsync();
            
            ViewBag.Campaigns = activeCampaigns;

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            ViewBag.TomTomApiKey = _tomTomOptions.ApiKey;

            return View(viewModel);
        }

        [Authorize]
        public async Task<IActionResult> Map()
        {
            var businesses = await _businessService.GetActiveBusinessesAsync();

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            ViewBag.TomTomApiKey = _tomTomOptions.ApiKey;

            return View(businesses);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Locations()
        {
            try
            {
                var locations = await _businessService.GetBusinessLocationsAsync();
                return Json(locations);
            }
            catch (Exception ex)
            {
                // Hata durumunda boş liste döndür ve logla
                return Json(new List<object>());
            }
        }

        // GET: Businesses/Create (Sadece Admin ve User)
        [Authorize(Roles = "Admin,User")]
        public IActionResult Create()
        {
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View();
        }

        // POST: Businesses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create(CreateBusinessViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
                return View(viewModel);
            }

            var business = _mapper.Map<Plafind.Models.Business>(viewModel);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = User.IsInRole("Admin");

            await _businessService.CreateBusinessAsync(business, userId, isAdmin);
            return RedirectToAction(nameof(Index));
        }

        // GET: Businesses/Edit/5 (Sadece Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var business = await _businessService.GetBusinessByIdAsync(id.Value);
            if (business == null) return NotFound();

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        // POST: Businesses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Plafind.Models.Business business)
        {
            if (id != business.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _businessService.UpdateBusinessAsync(business);
                }
                catch (Exception)
                {
                    if (await _businessService.GetBusinessByIdAsync(business.Id) == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        // GET: Businesses/Delete/5 (Sadece Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var business = await _businessService.GetBusinessByIdAsync(id.Value);
            if (business == null) return NotFound();

            return View(business);
        }

        // POST: Businesses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _businessService.DeleteBusinessAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}

