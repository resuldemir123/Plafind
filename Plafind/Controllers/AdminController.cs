using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Plafind.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Collections.Generic;

namespace Plafind.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly GoogleMapsOptions _mapsOptions;

        public AdminController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<ApplicationUser> signInManager,
            IOptions<GoogleMapsOptions> mapsOptions)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
        }

        public async Task<IActionResult> Index()
        {
            var totalBusinesses = await _context.Businesses.CountAsync();
            var activeBusinesses = await _context.Businesses.CountAsync(b => b.IsActive);
            var pendingApprovals = await _context.Businesses.CountAsync(b => !b.IsApproved);
            var totalUsers = await _context.Users.CountAsync();
            var totalReviews = await _context.Reviews.CountAsync();
            var pendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved);
            var totalFavorites = await _context.UserFavorites.CountAsync();
            var businessOwners = await _userManager.GetUsersInRoleAsync("BusinessOwner");
            var totalBusinessOwners = businessOwners.Count;

            // Son 7 günün istatistikleri
            var sevenDaysAgo = DateTime.Now.AddDays(-7);
            var newBusinessesThisWeek = await _context.Businesses.CountAsync(b => b.CreatedDate >= sevenDaysAgo);
            var newUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedDate >= sevenDaysAgo);
            var newReviewsThisWeek = await _context.Reviews.CountAsync(r => r.CreatedDate >= sevenDaysAgo);

            // Kategori bazında işletme sayıları
            var businessesByCategory = await _context.Businesses
                .Include(b => b.Category)
                .GroupBy(b => b.Category != null ? b.Category.Name : "Kategori Yok")
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var stats = new
            {
                TotalBusinesses = totalBusinesses,
                ActiveBusinesses = activeBusinesses,
                PendingApprovals = pendingApprovals,
                TotalUsers = totalUsers,
                TotalReviews = totalReviews,
                PendingReviews = pendingReviews,
                TotalFavorites = totalFavorites,
                TotalBusinessOwners = totalBusinessOwners,
                NewBusinessesThisWeek = newBusinessesThisWeek,
                NewUsersThisWeek = newUsersThisWeek,
                NewReviewsThisWeek = newReviewsThisWeek,
                BusinessesByCategory = businessesByCategory
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
                .Include(b => b.Owner)
                .OrderByDescending(b => b.CreatedDate)
                .ToListAsync();
            
            // İşletme sahipleri listesi (atama için)
            var businessOwners = await _userManager.GetUsersInRoleAsync("BusinessOwner");
            ViewBag.BusinessOwners = businessOwners;
            
            return View(businesses);
        }

        public IActionResult CreateBusiness()
        {
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
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
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }

        public async Task<IActionResult> EditBusiness(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null) return NotFound();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
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
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
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

        // İşletme Onaylama
        [HttpPost]
        public async Task<IActionResult> ApproveBusiness(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null) return NotFound();

            business.IsApproved = true;
            business.IsActive = true;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            await LogAdminAction("Approve", "Business", id.ToString(), $"İşletme onaylandı: {business.Name}");

            TempData["Success"] = "İşletme başarıyla onaylandı ve yayınlandı.";
            return RedirectToAction(nameof(Businesses));
        }

        // İşletme Reddetme
        [HttpPost]
        public async Task<IActionResult> RejectBusiness(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null) return NotFound();

            business.IsApproved = false;
            business.IsActive = false;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            await LogAdminAction("Reject", "Business", id.ToString(), $"İşletme reddedildi: {business.Name}");

            TempData["Success"] = "İşletme reddedildi.";
            return RedirectToAction(nameof(Businesses));
        }

        public async Task<IActionResult> Users()
        {
            var users = await _context.Users
                .Include(u => u.Reviews)
                .Include(u => u.Favorites)
                .Include(u => u.OwnedBusinesses)
                .OrderByDescending(u => u.CreatedDate)
                .ToListAsync();

            return View(users);
        }

        // İşletme Sahipleri Yönetimi
        public async Task<IActionResult> BusinessOwners()
        {
            var businessOwners = await _userManager.GetUsersInRoleAsync("BusinessOwner");
            var ownersWithBusinesses = new List<object>();

            foreach (var owner in businessOwners)
            {
                var businesses = await _context.Businesses
                    .Where(b => b.OwnerId == owner.Id)
                    .ToListAsync();

                ownersWithBusinesses.Add(new
                {
                    Owner = owner,
                    BusinessCount = businesses.Count,
                    ActiveBusinessCount = businesses.Count(b => b.IsActive),
                    PendingApprovalCount = businesses.Count(b => !b.IsApproved)
                });
            }

            ViewBag.OwnersWithBusinesses = ownersWithBusinesses;
            return View(businessOwners);
        }

        [HttpPost]
        public async Task<IActionResult> AssignBusinessToOwner(int businessId, string ownerId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null) return NotFound();

            var owner = await _userManager.FindByIdAsync(ownerId);
            if (owner == null || !await _userManager.IsInRoleAsync(owner, "BusinessOwner"))
            {
                TempData["Error"] = "Geçersiz işletme sahibi.";
                return RedirectToAction(nameof(Businesses));
            }

            business.OwnerId = ownerId;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            await LogAdminAction("Assign", "Business", businessId.ToString(), $"İşletme '{business.Name}' kullanıcı '{owner.Email}' adresine atandı.");

            TempData["Success"] = "İşletme başarıyla atandı.";
            return RedirectToAction(nameof(Businesses));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveBusinessFromOwner(int businessId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null) return NotFound();

            business.OwnerId = null;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            await LogAdminAction("Unassign", "Business", businessId.ToString(), $"İşletme '{business.Name}' sahibinden alındı.");

            TempData["Success"] = "İşletme sahibinden alındı.";
            return RedirectToAction(nameof(Businesses));
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

        // ==================== AUTHENTICATION MODULE ====================
        public async Task<IActionResult> Profile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (result.Succeeded)
                {
                    await LogAdminAction("ChangePassword", "User", user.Id, "Şifre değiştirildi");
                    TempData["Success"] = "Şifre başarıyla değiştirildi.";
                    return RedirectToAction(nameof(Profile));
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        public IActionResult TwoFactor()
        {
            return View();
        }

        // ==================== USER MANAGEMENT MODULE ====================
        public async Task<IActionResult> BannedUsers()
        {
            var users = await _context.Users
                .Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> BanUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(1));
                await LogAdminAction("Ban", "User", userId, $"Kullanıcı banlandı: {user.UserName}");
                TempData["Success"] = "Kullanıcı başarıyla banlandı.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        public async Task<IActionResult> UnbanUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                await LogAdminAction("Unban", "User", userId, $"Kullanıcı banı kaldırıldı: {user.UserName}");
                TempData["Success"] = "Kullanıcı banı kaldırıldı.";
            }
            return RedirectToAction(nameof(BannedUsers));
        }

        // ==================== ROLE & PERMISSION MODULE ====================
        public async Task<IActionResult> Roles()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (!string.IsNullOrWhiteSpace(roleName))
            {
                var role = new IdentityRole(roleName);
                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    await LogAdminAction("Create", "Role", roleName, $"Rol oluşturuldu: {roleName}");
                    TempData["Success"] = "Rol başarıyla oluşturuldu.";
                }
            }
            return RedirectToAction(nameof(Roles));
        }

        public async Task<IActionResult> Permissions()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            return View(roles);
        }

        public async Task<IActionResult> AssignRoles()
        {
            var users = await _userManager.Users.ToListAsync();
            var roles = await _roleManager.Roles.ToListAsync();
            
            var userRoles = new List<UserRoleViewModel>();
            foreach (var user in users)
            {
                var userRole = new UserRoleViewModel
                {
                    UserId = user.Id ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = await _userManager.GetRolesAsync(user)
                };
                userRoles.Add(userRole);
            }

            ViewBag.AllRoles = roles;
            return View(userRoles);
        }

        [HttpPost]
        public async Task<IActionResult> AssignRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrWhiteSpace(roleName))
            {
                await _userManager.AddToRoleAsync(user, roleName);
                await LogAdminAction("AssignRole", "User", userId, $"Kullanıcıya rol atandı: {roleName}");
                TempData["Success"] = "Rol başarıyla atandı.";
            }
            return RedirectToAction(nameof(AssignRoles));
        }

        [HttpPost]
        public async Task<IActionResult> RemoveRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !string.IsNullOrWhiteSpace(roleName))
            {
                await _userManager.RemoveFromRoleAsync(user, roleName);
                await LogAdminAction("RemoveRole", "User", userId, $"Kullanıcıdan rol kaldırıldı: {roleName}");
                TempData["Success"] = "Rol başarıyla kaldırıldı.";
            }
            return RedirectToAction(nameof(AssignRoles));
        }

        // ==================== CONTENT MANAGEMENT MODULE ====================
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            return View(categories);
        }

        public async Task<IActionResult> News()
        {
            var news = await _context.News
                .Include(n => n.Author)
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();
            return View(news);
        }

        // ==================== ANALYTICS MODULE ====================
        public async Task<IActionResult> Analytics()
        {
            var stats = new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalBusinesses = await _context.Businesses.CountAsync(),
                TotalReviews = await _context.Reviews.CountAsync(),
                ActiveBusinesses = await _context.Businesses.CountAsync(b => b.IsActive),
                PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved),
                RecentUsers = await _context.Users
                    .OrderByDescending(u => u.CreatedDate)
                    .Take(10)
                    .ToListAsync()
            };

            ViewBag.Stats = stats;
            return View();
        }

        public async Task<IActionResult> Reports()
        {
            var reports = new
            {
                BusinessReport = await _context.Businesses
                    .GroupBy(b => b.Category)
                    .Select(g => new { Category = g.Key != null ? g.Key.Name : "Kategori Yok", Count = g.Count() })
                    .ToListAsync(),
                ReviewReport = await _context.Reviews
                    .GroupBy(r => r.Rating)
                    .Select(g => new { Rating = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Rating)
                    .ToListAsync()
            };

            ViewBag.Reports = reports;
            return View();
        }

        // ==================== SYSTEM SETTINGS MODULE ====================
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Settings(SiteSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                await LogAdminAction("Update", "Settings", "Site", "Site ayarları güncellendi");
                TempData["Success"] = "Ayarlar başarıyla kaydedildi.";
                return RedirectToAction(nameof(Settings));
            }
            return View(model);
        }

        public IActionResult EmailSettings()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> EmailSettings(EmailSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                await LogAdminAction("Update", "Settings", "Email", "E-posta ayarları güncellendi");
                TempData["Success"] = "E-posta ayarları başarıyla kaydedildi.";
                return RedirectToAction(nameof(EmailSettings));
            }
            return View(model);
        }

        public IActionResult Backup()
        {
            return View();
        }

        // ==================== MEDIA MANAGER MODULE ====================
        public IActionResult Media()
        {
            // Bu kısım dosya sisteminden okunabilir veya veritabanından
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                await LogAdminAction("Upload", "Media", uniqueFileName, $"Dosya yüklendi: {file.FileName}");
                TempData["Success"] = "Dosya başarıyla yüklendi.";
            }
            return RedirectToAction(nameof(Media));
        }

        // ==================== LOGS & TRACKING MODULE ====================
        public async Task<IActionResult> Logs()
        {
            var logs = await _context.AdminLogs
                .OrderByDescending(l => l.CreatedDate)
                .Take(100)
                .ToListAsync();
            
            // Admin kullanıcı bilgilerini ekle
            var adminUsersDict = new Dictionary<string, ApplicationUser?>();
            foreach (var log in logs)
            {
                if (!string.IsNullOrEmpty(log.AdminUserId))
                {
                    var adminUser = await _userManager.FindByIdAsync(log.AdminUserId);
                    if (adminUser != null)
                    {
                        adminUsersDict[log.AdminUserId] = adminUser;
                    }
                }
            }
            ViewBag.AdminUsers = adminUsersDict;
            
            return View(logs);
        }

        public async Task<IActionResult> ActivityLog()
        {
            var logs = await _context.AdminLogs
                .OrderByDescending(l => l.CreatedDate)
                .Take(50)
                .ToListAsync();
            
            // Admin kullanıcı bilgilerini ekle
            var adminUsersDict = new Dictionary<string, ApplicationUser?>();
            foreach (var log in logs)
            {
                if (!string.IsNullOrEmpty(log.AdminUserId))
                {
                    var adminUser = await _userManager.FindByIdAsync(log.AdminUserId);
                    if (adminUser != null)
                    {
                        adminUsersDict[log.AdminUserId] = adminUser;
                    }
                }
            }
            ViewBag.AdminUsers = adminUsersDict;
            
            return View(logs);
        }

        // ==================== FEEDBACK & SUPPORT MODULE ====================
        public async Task<IActionResult> Messages()
        {
            // Contact form mesajları - şimdilik boş liste döndürüyoruz
            // İleride ContactMessage tablosu eklendiğinde aktif edilebilir
            var messages = new List<ContactMessage>();
            return View(messages);
        }

        public IActionResult Tickets()
        {
            // Ticket sistemi buraya gelecek
            return View();
        }

        // ==================== HELPER METHODS ====================
        private async Task LogAdminAction(string action, string entityType, object entityId, string description)
        {
            try
            {
                var log = new AdminLog
                {
                    AdminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId?.ToString(),
                    Description = description,
                    CreatedDate = DateTime.Now
                };

                _context.AdminLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Log hatası durumunda sessizce devam et
            }
        }
    }
}
