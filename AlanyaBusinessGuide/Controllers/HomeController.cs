using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AlanyaBusinessGuide.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["Title"] = "Alanya İşletme Rehberi - Ana Sayfa";

                var featuredBusinesses = await _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved && b.IsFeatured)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .OrderByDescending(b => b.AverageRating)
                    .Take(6)
                    .ToListAsync();

                var topRatedBusinesses = await _context.Businesses
                    .Where(b => b.IsActive && b.IsApproved)
                    .Include(b => b.Category)
                    .Include(b => b.Reviews)
                    .OrderByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.TotalReviews)
                    .Take(6)
                    .ToListAsync();

                var categories = await _context.Categories
                    .Where(c => c.Businesses.Any(b => b.IsActive && b.IsApproved))
                    .Select(c => c.Name)
                    .Distinct()
                    .Take(8)
                    .ToListAsync();

                var latestNews = await _context.News
                    .Include(n => n.Author)
                    .OrderByDescending(n => n.PublishDate)
                    .Take(3)
                    .ToListAsync();

                ViewBag.FeaturedBusinesses = featuredBusinesses;
                ViewBag.TopRatedBusinesses = topRatedBusinesses;
                ViewBag.Categories = categories;
                ViewBag.LatestNews = latestNews;

                return View();
            }
            catch (Exception)
            {
                // Hata loglama eklenebilir (örneğin, ILogger ile)
                return View("Error"); // Hata sayfasına yönlendirme
            }
        }

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

                ViewBag.Query = query;
                ViewBag.Category = category;
                ViewBag.MinRating = minRating;
                ViewBag.PriceRange = priceRange;
                ViewBag.SortBy = sortBy;
                
                // Get all categories for filter dropdown
                ViewBag.Categories = await _context.Categories
                    .Where(c => c.Businesses.Any(b => b.IsActive && b.IsApproved))
                    .Select(c => c.Name)
                    .Distinct()
                    .ToListAsync();

                return View(businesses);
            }
            catch (Exception)
            {
                // Hata loglama eklenebilir
                return View("Error");
            }
        }

        public IActionResult Contact()
        {
            ViewData["Title"] = "İletişim - Alanya İşletme Rehberi";
            return View();
        }
    }
}