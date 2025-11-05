using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using AlanyaBusinessGuide.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AlanyaBusinessGuide.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        public async Task<IActionResult> Businesses(string? category, string? search)
        {
            var query = _context.Businesses
                .Where(b => b.IsActive && b.IsApproved)
                .Include(b => b.Reviews)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(b => b.CategoryId != null && b.Category!.Name == category); // Category.Name ile karşılaştırma
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => !string.IsNullOrEmpty(b.Name) && b.Name.Contains(search) ||
                                        !string.IsNullOrEmpty(b.Description) && b.Description.Contains(search));
            }

            var businesses = await query
                .OrderByDescending(b => b.IsFeatured)
                .ThenByDescending(b => b.AverageRating)
                .ToListAsync();

            ViewBag.Categories = await _context.Categories // Doğrudan Categories tablosundan al
                .Where(c => c.Businesses.Any(b => b.IsActive && b.IsApproved))
                .Select(c => c.Name)
                .Distinct()
                .ToListAsync();

            return View(businesses);
        }

        public async Task<IActionResult> BusinessDetails(int id)
        {
            var business = await _context.Businesses
                .Include(b => b.Reviews.Where(r => r.IsApproved && r.IsActive))
                .ThenInclude(r => r.User)
                .Include(b => b.Favorites)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (business == null) return NotFound();

            var user = await GetCurrentApplicationUserAsync();
            if (user != null)
            {
                ViewBag.IsFavorite = await _context.UserFavorites
                    .AnyAsync(f => f.UserId == user.Id && f.BusinessId == id);
            }

            return View(business);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int businessId)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var existingFavorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == user.Id && f.BusinessId == businessId);

            if (existingFavorite != null)
            {
                _context.UserFavorites.Remove(existingFavorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = false, message = "Favorilerden çıkarıldı" });
            }
            else
            {
                var favorite = new UserFavorite
                {
                    UserId = user.Id,
                    BusinessId = businessId,
                    AddedDate = DateTime.Now
                };
                _context.UserFavorites.Add(favorite);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isFavorite = true, message = "Favorilere eklendi" });
            }
        }

        public async Task<IActionResult> Favorites()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var favorites = await _context.UserFavorites
                .Include(f => f.Business)
                .ThenInclude(b => b.Reviews)
                .Where(f => f.UserId == user.Id)
                .OrderByDescending(f => f.AddedDate)
                .ToListAsync();

            return View(favorites);
        }

        [HttpPost]
        public async Task<IActionResult> AddReview(int businessId, string comment, int rating)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == user.Id && r.BusinessId == businessId);

            if (existingReview != null)
            {
                return Json(new { success = false, message = "Bu işletmeye zaten yorum yaptınız" });
            }

            if (string.IsNullOrEmpty(comment) || rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Geçersiz yorum veya puan" });
            }

            var review = new Review
            {
                BusinessId = businessId,
                UserId = user.Id,
                Comment = comment,
                Rating = rating,
                CreatedDate = DateTime.Now,
                IsApproved = true,
                IsActive = true
            };

            _context.Reviews.Add(review);

            var business = await _context.Businesses.FindAsync(businessId);
            if (business != null)
            {
                var allReviews = await _context.Reviews
                    .Where(r => r.BusinessId == businessId && r.IsApproved && r.IsActive)
                    .ToListAsync();

                business.AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
                business.TotalReviews = allReviews.Count;
                _context.Businesses.Update(business);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yorumunuz eklendi" });
        }

        public async Task<IActionResult> MyReviews()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var reviews = await _context.Reviews
                .Include(r => r.Business)
                .Where(r => r.UserId == user.Id)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            return View(reviews);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == user.Id);

            if (review == null) return Json(new { success = false, message = "Yorum bulunamadı" });

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yorum silindi" });
        }

        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                user.FullName = model.FullName;
                user.PhoneNumber = model.PhoneNumber;
                await _userManager.UpdateAsync(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Profiliniz güncellendi";
                return RedirectToAction(nameof(Profile));
            }

            return View("Profile", user);
        }

        private async Task<ApplicationUser> GetCurrentApplicationUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) : null;
        }
    }
}