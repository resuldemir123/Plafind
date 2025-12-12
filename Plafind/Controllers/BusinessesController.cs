using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Plafind.Options;
using Plafind.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Plafind.Controllers
{
    public class BusinessesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleMapsOptions _mapsOptions;
        private readonly TomTomOptions _tomTomOptions;
        private readonly IConfiguration _configuration;

        public BusinessesController(ApplicationDbContext context, IOptions<GoogleMapsOptions> mapsOptions, IOptions<TomTomOptions> tomTomOptions, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
            _tomTomOptions = tomTomOptions?.Value ?? new TomTomOptions();
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // GET: Businesses (Herkes görebilir)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var businesses = await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Include(b => b.Favorites)
                .ToListAsync();
            return View(businesses);
        }

        // GET: Businesses/Details/5 (Herkes görebilir)
        [AllowAnonymous]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var business = await _context.Businesses
                .Include(b => b.Category)
                .Include(b => b.Reviews.Where(r => r.IsActive && r.IsApproved))
                    .ThenInclude(r => r.User)
                .Include(b => b.Favorites)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (business == null)
            {
                return NotFound();
            }

            var siteUrl = _configuration["SiteSettings:SiteUrl"] ?? "https://plafind.com";
            var siteName = _configuration["SiteSettings:SiteName"] ?? "Plafind";
            
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

            var similarBusinesses = new List<Business>();

            if (business.CategoryId.HasValue)
            {
                similarBusinesses = await _context.Businesses
                    .Where(b => b.Id != business.Id &&
                                b.CategoryId == business.CategoryId &&
                                b.IsActive &&
                                b.IsApproved)
                    .Include(b => b.Category)
                    .OrderByDescending(b => b.IsFeatured)
                    .ThenByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.CreatedDate)
                    .Take(6)
                    .ToListAsync();
            }

            // Gerçek yorum sayısını hesapla (IsActive && IsApproved olanlar)
            var activeApprovedReviewsCount = business.Reviews?.Count(r => r.IsActive && r.IsApproved) ?? 0;
            ViewBag.ActiveApprovedReviewsCount = activeApprovedReviewsCount;

            var viewModel = new BusinessDetailsViewModel
            {
                Business = business,
                SimilarBusinesses = similarBusinesses
            };

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            ViewBag.TomTomApiKey = _tomTomOptions.ApiKey;

            return View(viewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Map()
        {
            var businesses = await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .ToListAsync();

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            ViewBag.TomTomApiKey = _tomTomOptions.ApiKey;

            return View(businesses);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Locations()
        {
            // Hem koordinatı olan hem de adres bilgisi olan işletmeleri getir
            var locations = await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved && 
                           (b.Latitude.HasValue && b.Longitude.HasValue || !string.IsNullOrEmpty(b.Address)))
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Address,
                    Category = b.Category != null ? b.Category.Name : null,
                    b.Phone,
                    b.ImageUrl,
                    b.AverageRating,
                    b.TotalReviews,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    HasCoordinates = b.Latitude.HasValue && b.Longitude.HasValue
                })
                .ToListAsync();

            return Json(locations);
        }

        // GET: Businesses/Create (Sadece Admin ve User)
        [Authorize(Roles = "Admin,User")]
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View();
        }

        // POST: Businesses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Create(Business business)
        {
            if (ModelState.IsValid)
            {
                business.IsApproved = User.IsInRole("Admin"); // Admin ise otomatik onaylı
                business.IsActive = true;
                _context.Add(business);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        // GET: Businesses/Edit/5 (Sadece Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var business = await _context.Businesses
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (business == null) return NotFound();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        // POST: Businesses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Business business)
        {
            if (id != business.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(business);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BusinessExists(business.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        // GET: Businesses/Delete/5 (Sadece Admin)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var business = await _context.Businesses
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (business == null) return NotFound();

            return View(business);
        }

        // POST: Businesses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business != null)
            {
                _context.Businesses.Remove(business);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool BusinessExists(int id)
        {
            return _context.Businesses.Any(e => e.Id == id);
        }
    }
}