using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using AlanyaBusinessGuide.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace AlanyaBusinessGuide.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var stats = new
            {
                TotalBusinesses = await _context.Businesses.CountAsync(),
                ActiveBusinesses = await _context.Businesses.CountAsync(b => b.IsActive),
                PendingApprovals = await _context.Businesses.CountAsync(b => !b.IsApproved),
                TotalUsers = await _context.Users.CountAsync(), // _context.ApplicationUsers yerine _context.Users
                TotalReviews = await _context.Reviews.CountAsync(),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved)
            };

            var recentActivities = await _context.Reviews
                .Include(r => r.User)
                .Include(r => r.Business)
                .OrderByDescending(r => r.CreatedDate)
                .Take(10)
                .ToListAsync();

            var recentFavorites = await _context.UserFavorites
                .Include(f => f.User)
                .Include(f => f.Business)
                .OrderByDescending(f => f.AddedDate)
                .Take(10)
                .ToListAsync();

            ViewBag.Stats = stats;
            ViewBag.RecentActivities = recentActivities;
            ViewBag.RecentFavorites = recentFavorites;
            return View();
        }

        public async Task<IActionResult> Businesses()
        {
            var businesses = await _context.Businesses
                .Include(b => b.Reviews)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
            return View(businesses);
        }

        public IActionResult CreateBusiness()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateBusiness(Business business)
        {
            if (ModelState.IsValid)
            {
                business.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
                business.IsApproved = true;
                _context.Businesses.Add(business);
                await _context.SaveChangesAsync();

                await LogAdminAction("Create", "Business", business.Id.ToString(), $"İşletme oluşturuldu: {business.Name}");

                return RedirectToAction(nameof(Businesses));
            }
            return View(business);
        }

        public async Task<IActionResult> EditBusiness(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null) return NotFound();
            return View(business);
        }

        [HttpPost]
        public async Task<IActionResult> EditBusiness(Business business)
        {
            if (ModelState.IsValid)
            {
                business.UpdatedDate = DateTime.Now;
                _context.Businesses.Update(business);
                await _context.SaveChangesAsync();

                await LogAdminAction("Update", "Business", business.Id.ToString(), $"İşletme güncellendi: {business.Name}");

                return RedirectToAction(nameof(Businesses));
            }
            return View(business);
        }

        public async Task<IActionResult> DeleteBusiness(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null) return NotFound();
            return View(business);
        }

        [HttpPost, ActionName("DeleteBusiness")]
        public async Task<IActionResult> DeleteBusinessConfirmed(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business != null)
            {
                _context.Businesses.Remove(business);
                await _context.SaveChangesAsync();

                await LogAdminAction("Delete", "Business", id.ToString(), $"İşletme silindi: {business.Name}");
            }
            return RedirectToAction(nameof(Businesses));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Reviews)
                .Include(u => u.Favorites)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync(); // _context.ApplicationUsers yerine _context.Users

            return View(users);
        }

        public async Task<IActionResult> UserDetails(string id)
        {
            var user = await _context.Users
                .Include(u => u.Reviews)
                .ThenInclude(r => r.Business)
                .Include(u => u.Favorites)
                .ThenInclude(f => f.Business)
                .FirstOrDefaultAsync(u => u.Id == id); // _context.ApplicationUsers yerine _context.Users

            if (user == null) return NotFound();
            return View(user);
        }

        public async Task<IActionResult> Reviews()
        {
            var reviews = await _context.Reviews
                .Include(r => r.Business)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
            return View(reviews);
        }

        public async Task<IActionResult> ApproveReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = true;
                _context.Reviews.Update(review);
                await _context.SaveChangesAsync();

                await LogAdminAction("Approve", "Review", review.Id.ToString(), "Yorum onaylandı");
            }
            return RedirectToAction(nameof(Reviews));
        }

        public async Task<IActionResult> RejectReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                review.IsApproved = false;
                review.IsActive = false;
                _context.Reviews.Update(review);
                await _context.SaveChangesAsync();

                await LogAdminAction("Reject", "Review", review.Id.ToString(), "Yorum reddedildi");
            }
            return RedirectToAction(nameof(Reviews));
        }

        public async Task<IActionResult> Favorites()
        {
            var favorites = await _context.UserFavorites
                .Include(f => f.User)
                .Include(f => f.Business)
                .OrderByDescending(f => f.AddedDate)
                .ToListAsync();
            return View(favorites);
        }

        private async Task LogAdminAction(string action, string entityType, object entityId, string description)
        {
            var log = new AdminLog
            {
                AdminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                Action = action,
                EntityType = entityType,
                EntityId = entityId?.ToString(),
                Description = description
            };

            _context.AdminLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}