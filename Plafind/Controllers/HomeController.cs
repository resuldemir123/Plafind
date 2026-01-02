using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Plafind.Models;
using Plafind.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Plafind.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public HomeController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["Title"] = "Alanya İşletme Rehberi - Ana Sayfa";

                var siteUrl = _configuration["SiteSettings:SiteUrl"] ?? "https://plafind.com";
                var siteName = _configuration["SiteSettings:SiteName"] ?? "Plafind";
                
                // Open Graph bilgilerini set et
                ViewData["SiteUrl"] = siteUrl;
                ViewData["SiteName"] = siteName;
                ViewData["OgTitle"] = "Plafind - Alanya İşletme Rehberi";
                ViewData["OgDescription"] = "Alanya'nın en kapsamlı işletme rehberi. Restoranlar, oteller, mağazalar ve daha fazlasını keşfedin. Deneyimlerinizi paylaşın ve favori yerlerinizi kaydedin.";
                ViewData["OgImage"] = $"{siteUrl}/images/Logo.png";
                ViewData["OgUrl"] = $"{siteUrl}";
                ViewData["OgType"] = "website";

                var featuredBusinesses = await _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved && b.IsFeatured)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                        .ThenInclude(r => r.User)
                    .OrderByDescending(b => b.AverageRating)
                    .Take(6)
                    .ToListAsync();

                var topRatedBusinesses = await _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                        .ThenInclude(r => r.User)
                    .OrderByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.TotalReviews)
                    .Take(6)
                    .ToListAsync();

                var categories = await _context.Categories
                    .Where(c => _context.Businesses.Any(b => b.CategoryId == c.Id && b.IsActive && b.IsApproved))
                    .Select(c => c.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .Take(8)
                    .ToListAsync();

                ViewBag.FeaturedBusinesses = featuredBusinesses;
                ViewBag.TopRatedBusinesses = topRatedBusinesses;
                ViewBag.Categories = categories;

                return View();
            }
            catch (Exception)
            {
                // Hata loglama eklenebilir (örneğin, ILogger ile)
                return RedirectToAction("Error"); // Hata sayfasına yönlendirme
            }
        }

        [Authorize]
        public async Task<IActionResult> Search(string? query, string? category, string? minRating, 
            string? priceRange, string? sortBy = "featured")
        {
            try
            {
                ViewData["Title"] = "Alanya İşletmeleri";

                var businessesQuery = _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .AsQueryable();

                // Text search
                if (!string.IsNullOrEmpty(query))
                {
                    businessesQuery = businessesQuery.Where(b =>
                        (!string.IsNullOrEmpty(b.Name) && b.Name.Contains(query)) ||
                        (!string.IsNullOrEmpty(b.Description) && b.Description.Contains(query)) ||
                        (!string.IsNullOrEmpty(b.Address) && b.Address.Contains(query)));
                }

                // Category filter
                if (!string.IsNullOrEmpty(category))
                {
                    businessesQuery = businessesQuery.Where(b => b.Category != null && b.Category.Name == category);
                }

                // Rating filter
                if (!string.IsNullOrEmpty(minRating) && double.TryParse(minRating, out double minRatingValue))
                {
                    businessesQuery = businessesQuery.Where(b => b.AverageRating >= minRatingValue);
                }

                // Price range filter
                if (!string.IsNullOrEmpty(priceRange))
                {
                    businessesQuery = businessesQuery.Where(b => b.PriceRange == priceRange);
                }

                // Apply sorting
                switch (sortBy?.ToLower())
                {
                    case "rating":
                        businessesQuery = businessesQuery.OrderByDescending(b => b.AverageRating);
                        break;
                    case "reviews":
                        businessesQuery = businessesQuery.OrderByDescending(b => b.TotalReviews);
                        break;
                    case "distance":
                        // For now, just order by name - distance calculation would require coordinates
                        businessesQuery = businessesQuery.OrderBy(b => b.Name);
                        break;
                    default: // "featured"
                        businessesQuery = businessesQuery
                            .OrderByDescending(b => b.IsFeatured)
                            .ThenByDescending(b => b.AverageRating);
                        break;
                }

                var businesses = await businessesQuery.ToListAsync();

                // FeaturesJson'ı deserialize edip Features listesine dönüştür
                foreach (var business in businesses)
                {
                    if (!string.IsNullOrEmpty(business.FeaturesJson))
                    {
                        try
                        {
                            business.Features = JsonSerializer.Deserialize<List<BusinessFeature>>(business.FeaturesJson);
                        }
                        catch
                        {
                            business.Features = new List<BusinessFeature>();
                        }
                    }
                    else
                    {
                        business.Features = new List<BusinessFeature>();
                    }
                }

                ViewBag.Query = query;
                ViewBag.Category = category;
                ViewBag.MinRating = minRating;
                ViewBag.PriceRange = priceRange;
                ViewBag.SortBy = sortBy;
                
                // Get all categories for filter dropdown
                ViewBag.Categories = await _context.Categories
                    .Where(c => _context.Businesses.Any(b => b.CategoryId == c.Id && b.IsActive && b.IsApproved))
                    .Select(c => c.Name)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return View(businesses);
            }
            catch (Exception)
            {
                // Hata loglama eklenebilir
                return RedirectToAction("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult Contact()
        {
            ViewData["Title"] = "İletişim - Alanya İşletme Rehberi";
            return View(new ContactViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            ViewData["Title"] = "İletişim - Alanya İşletme Rehberi";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Veritabanına kaydet
                var contactMessage = new ContactMessage
                {
                    Name = model.Name,
                    Email = model.Email,
                    Phone = model.Phone,
                    Subject = model.Subject,
                    Message = model.Message,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                _context.ContactMessages.Add(contactMessage);
                await _context.SaveChangesAsync();

                // E-posta gönder
                var emailSent = await _emailService.SendContactEmailAsync(
                    model.Name,
                    model.Email,
                    model.Phone,
                    model.Subject,
                    model.Message
                );

                if (emailSent)
                {
                    TempData["SuccessMessage"] = "Mesajınız başarıyla gönderildi! En kısa sürede size dönüş yapacağız.";
                    return RedirectToAction(nameof(Contact));
                }
                else
                {
                    TempData["SuccessMessage"] = "Mesajınız kaydedildi ancak e-posta gönderilemedi. En kısa sürede size dönüş yapacağız.";
                    return RedirectToAction(nameof(Contact));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.");
            }

            return View(model);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            ViewData["Title"] = "Gizlilik Politikası - Alanya İşletme Rehberi";
            return View();
        }

        [AllowAnonymous]
        public IActionResult Help()
        {
            ViewData["Title"] = "Yardım - Alanya İşletme Rehberi";
            return View();
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            ViewData["Title"] = "Hakkımızda - Alanya İşletme Rehberi";
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            // Kullanıcının tercih ettiği dili çereze kaydet
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions 
                { 
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    HttpOnly = true,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                }
            );

            // Güvenlik: returnUrl'in local olduğunu doğrula
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            
            // Geçersiz veya boş returnUrl durumunda ana sayfaya yönlendir
            return RedirectToAction("Index", "Home");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [AllowAnonymous]
        public IActionResult Error()
        {
            var model = new ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View(model);
        }
    }
}