using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using AlanyaBusinessGuide.Models;
using AlanyaBusinessGuide.Options;
using AlanyaBusinessGuide.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AlanyaBusinessGuide.Controllers
{
    public class BusinessesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly GoogleMapsOptions _mapsOptions;

        public BusinessesController(ApplicationDbContext context, IOptions<GoogleMapsOptions> mapsOptions)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
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
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .Include(b => b.Favorites)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (business == null)
            {
                return NotFound();
            }

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

            var viewModel = new BusinessDetailsViewModel
            {
                Business = business,
                SimilarBusinesses = similarBusinesses
            };

            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;

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

            return View(businesses);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Locations()
        {
            var locations = await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved && b.Latitude.HasValue && b.Longitude.HasValue)
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
                    Longitude = b.Longitude
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