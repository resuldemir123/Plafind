using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using AlanyaBusinessGuide.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AlanyaBusinessGuide.ViewModels;
using Microsoft.AspNetCore.Hosting; 
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Linq;
using AlanyaBusinessGuide.Services;

namespace AlanyaBusinessGuide.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IEmailService _emailService;

        private static readonly string[] _defaultAvatars = new[]
        {
            "/images/avatars/avatar-1.svg",
            "/images/avatars/avatar-2.svg",
            "/images/avatars/avatar-3.svg",
            "/images/avatars/avatar-4.svg"
        };

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment,
            IEmailService emailService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
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
                .Where(c => c.Businesses != null && c.Businesses.Any(b => b.IsActive && b.IsApproved))
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

        [HttpGet]
        public async Task<IActionResult> CheckFavorite(int businessId)
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null) return Json(new { isFavorite = false });

            var isFavorite = await _context.UserFavorites
                .AnyAsync(f => f.UserId == user.Id && f.BusinessId == businessId);

            return Json(new { isFavorite });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
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
                .ThenInclude(b => b.Reviews!)
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Favorites)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var photos = await _context.UserPhotos
                .Where(p => p.UserId == userId && p.IsActive)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var model = new UserProfileViewModel
            {
                UserName = user.UserName ?? string.Empty,
                Email = user.Email,
                DisplayName = user.DisplayName ?? user.FullName ?? user.UserName,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                City = user.City,
                Country = user.Country,
                Website = user.Website,
                Bio = user.Bio,
                CurrentAvatarUrl = ResolveAvatarUrl(user.AvatarUrl),
                DefaultAvatars = _defaultAvatars,
                CreatedDate = user.CreatedDate,
                FavoritesCount = user.Favorites?.Count ?? 0,
                ReviewsCount = user.Reviews?.Count ?? 0,
                Photos = photos,
                SuccessMessage = TempData["SuccessMessage"] as string,
                ErrorMessage = TempData["ErrorMessage"] as string
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UserProfileViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                .Include(u => u.Favorites)
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            model.DefaultAvatars = _defaultAvatars;

            if (!ModelState.IsValid)
            {
                PopulateStaticFields(user, model);
                return View("Profile", model);
            }

            // Handle username change
            if (!string.IsNullOrWhiteSpace(model.UserName) &&
                !string.Equals(model.UserName, user.UserName, StringComparison.OrdinalIgnoreCase))
            {
                var existingUser = await _userManager.FindByNameAsync(model.UserName);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError(nameof(model.UserName), "Bu kullanıcı adı zaten kullanımda.");
                    PopulateStaticFields(user, model);
                    return View("Profile", model);
                }

                var setUserNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var error in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(nameof(model.UserName), error.Description);
                    }
                    PopulateStaticFields(user, model);
                    return View("Profile", model);
                }
            }

            user.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
                ? user.UserName
                : model.DisplayName.Trim();
            user.FullName = model.FullName?.Trim();
            user.PhoneNumber = model.PhoneNumber;
            user.City = model.City?.Trim();
            user.Country = model.Country?.Trim();
            user.Website = model.Website?.Trim();
            user.Bio = model.Bio?.Trim();

            if (model.AvatarFile != null && model.AvatarFile.Length > 2 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(model.AvatarFile), "Avatar dosyası 2 MB boyutunu geçmemelidir.");
                PopulateStaticFields(user, model);
                return View("Profile", model);
            }

            var avatarUpdate = await ProcessAvatarAsync(model, user);
            if (avatarUpdate.hasUpdate)
            {
                UpdateAvatar(user, avatarUpdate.path);
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                PopulateStaticFields(user, model);
                return View("Profile", model);
            }

            await _context.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "Profiliniz güncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        private void PopulateStaticFields(ApplicationUser user, UserProfileViewModel model)
        {
            model.CurrentAvatarUrl = ResolveAvatarUrl(user.AvatarUrl);
            model.CreatedDate = user.CreatedDate;
            model.FavoritesCount = user.Favorites?.Count ?? 0;
            model.ReviewsCount = user.Reviews?.Count ?? 0;
            model.DefaultAvatars = _defaultAvatars;
            model.SelectedAvatar = null;
            model.City = user.City;
            model.Country = user.Country;
            model.Email = user.Email;
            model.Website = user.Website;
            model.Bio = user.Bio;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
        {
            if (model == null || !ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Lütfen geçerli bir e-posta ve şifre girin.";
                return RedirectToAction(nameof(Profile));
            }

            var user = await GetCurrentApplicationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword!);
            if (!passwordValid)
            {
                TempData["ErrorMessage"] = "Mevcut şifreniz hatalı.";
                return RedirectToAction(nameof(Profile));
            }

            if (string.Equals(user.Email, model.NewEmail, StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Yeni e-posta mevcut e-posta ile aynı olamaz.";
                return RedirectToAction(nameof(Profile));
            }

            var existingUser = await _userManager.FindByEmailAsync(model.NewEmail!);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                TempData["ErrorMessage"] = "Bu e-posta başka bir kullanıcı tarafından kullanılıyor.";
                return RedirectToAction(nameof(Profile));
            }

            var setEmailResult = await _userManager.SetEmailAsync(user, model.NewEmail!);
            if (!setEmailResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", setEmailResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            await _context.SaveChangesAsync();
            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] = "E-posta adresiniz başarıyla güncellendi.";
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestPasswordReset()
        {
            var user = await GetCurrentApplicationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                TempData["ErrorMessage"] = "E-posta adresiniz kayıtlı değil. Lütfen önce e-posta adresinizi güncelleyin.";
                return RedirectToAction(nameof(Profile));
            }

            try
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, token = token }, protocol: Request.Scheme);

                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #ffc107 0%, #ff9800 100%); color: white; padding: 20px; border-radius: 8px 8px 0 0; text-align: center; }}
        .content {{ background: #f9f9f9; padding: 20px; border: 1px solid #ddd; border-top: none; border-radius: 0 0 8px 8px; }}
        .button {{ display: inline-block; padding: 12px 30px; background: #ffc107; color: #212529; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .button:hover {{ background: #ff9800; }}
        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
        .warning {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>Şifre Sıfırlama İsteği</h2>
        </div>
        <div class='content'>
            <p>Merhaba <strong>{user.DisplayName ?? user.UserName}</strong>,</p>
            <p>Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
            <div style='text-align: center;'>
                <a href='{callbackUrl}' class='button'>Şifremi Sıfırla</a>
            </div>
            <div class='warning'>
                <strong>Güvenlik Uyarısı:</strong> Bu link 24 saat geçerlidir. Eğer bu isteği siz yapmadıysanız, lütfen bu e-postayı görmezden gelin.
            </div>
            <p>Eğer buton çalışmıyorsa, aşağıdaki linki tarayıcınıza kopyalayıp yapıştırabilirsiniz:</p>
            <p style='word-break: break-all; color: #666; font-size: 12px;'>{callbackUrl}</p>
            <div class='footer'>
                <p>Bu bir otomatik e-postadır. Lütfen bu e-postaya yanıt vermeyin.</p>
            </div>
        </div>
    </div>
</body>
</html>";

                var emailSent = await _emailService.SendEmailAsync(
                    user.Email,
                    "Şifre Sıfırlama İsteği - Alanya İşletme Rehberi",
                    emailBody
                );

                if (emailSent)
                {
                    TempData["SuccessMessage"] = "Şifre sıfırlama linki e-posta adresinize gönderildi. Lütfen e-postanızı kontrol edin.";
                }
                else
                {
                    TempData["ErrorMessage"] = "E-posta gönderilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhotos(IFormFile[] photoFiles)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            if (photoFiles == null || photoFiles.Length == 0)
            {
                TempData["ErrorMessage"] = "Lütfen en az bir fotoğraf seçin.";
                return RedirectToAction(nameof(Profile));
            }

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "photos");
            Directory.CreateDirectory(uploadsPath);

            var uploadedCount = 0;
            var errors = new List<string>();

            foreach (var file in photoFiles)
            {
                if (file == null || file.Length == 0)
                    continue;

                if (file.Length > 5 * 1024 * 1024) // 5MB
                {
                    errors.Add($"{file.FileName} dosyası çok büyük (maks. 5MB)");
                    continue;
                }

                var extension = Path.GetExtension(file.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !IsSupportedImageExtension(extension))
                {
                    errors.Add($"{file.FileName} desteklenmeyen format");
                    continue;
                }

                try
                {
                    var fileName = $"{userId}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var photo = new UserPhoto
                    {
                        UserId = userId,
                        PhotoUrl = $"/uploads/photos/{fileName}",
                        Description = $"Bölgede çekilen fotoğraf - {DateTime.Now:dd.MM.yyyy}",
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    _context.UserPhotos.Add(photo);
                    uploadedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName} yüklenirken hata: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();

            if (uploadedCount > 0)
            {
                TempData["SuccessMessage"] = $"{uploadedCount} fotoğraf başarıyla yüklendi.";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] = string.Join("; ", errors);
            }

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePhoto(int photoId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });
            }

            var photo = await _context.UserPhotos
                .FirstOrDefaultAsync(p => p.Id == photoId && p.UserId == userId);

            if (photo == null)
            {
                return Json(new { success = false, message = "Fotoğraf bulunamadı" });
            }

            try
            {
                // Fiziksel dosyayı sil
                if (!string.IsNullOrEmpty(photo.PhotoUrl) && photo.PhotoUrl.StartsWith("/uploads/photos/"))
                {
                    var filePath = Path.Combine(_environment.WebRootPath, photo.PhotoUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.UserPhotos.Remove(photo);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Fotoğraf silindi" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        private string ResolveAvatarUrl(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                return _defaultAvatars.First();
            }

            if (avatarUrl.StartsWith("~/"))
            {
                return avatarUrl.Substring(1);
            }

            return avatarUrl;
        }

        private async Task<(bool hasUpdate, string? path)> ProcessAvatarAsync(UserProfileViewModel model, ApplicationUser user)
        {
            if (model.AvatarFile != null && model.AvatarFile.Length > 0)
            {
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(uploadsPath);

                var extension = Path.GetExtension(model.AvatarFile.FileName);
                if (string.IsNullOrWhiteSpace(extension) || !IsSupportedImageExtension(extension))
                {
                    extension = ".png";
                }

                var fileName = $"{user.Id}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.AvatarFile.CopyToAsync(stream);
                }

                return (true, $"/uploads/avatars/{fileName}");
            }

            if (!string.IsNullOrWhiteSpace(model.SelectedAvatar))
            {
                if (model.SelectedAvatar == "remove")
                {
                    return (true, null);
                }

                if (_defaultAvatars.Contains(model.SelectedAvatar))
                {
                    return (true, model.SelectedAvatar);
                }
            }

            return (false, null);
        }

        private bool IsSupportedImageExtension(string extension)
        {
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };
            return allowed.Contains(extension.ToLowerInvariant());
        }

        private void UpdateAvatar(ApplicationUser user, string? newPath)
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarUrl) &&
                user.AvatarUrl.StartsWith("/uploads/avatars", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(user.AvatarUrl, newPath, StringComparison.OrdinalIgnoreCase))
            {
                var existingPath = Path.Combine(_environment.WebRootPath, user.AvatarUrl.TrimStart('/', '\\'));
                if (System.IO.File.Exists(existingPath))
                {
                    System.IO.File.Delete(existingPath);
                }
            }

            user.AvatarUrl = newPath;
        }

        private async Task<ApplicationUser?> GetCurrentApplicationUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId != null ? await _context.Users.FirstOrDefaultAsync(u => u.Id == userId) : null;
        }
    }
}