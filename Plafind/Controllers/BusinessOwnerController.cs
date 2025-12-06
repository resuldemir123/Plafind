using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using Plafind.Options;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Plafind.Controllers
{
    public class BusinessOwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly GoogleMapsOptions _mapsOptions;
        private readonly RoleManager<IdentityRole> _roleManager;

        public BusinessOwnerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            IOptions<GoogleMapsOptions> mapsOptions)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        // İşletme Kayıt Formu (Giriş yapmamış kullanıcılar için)
        [AllowAnonymous]
        public IActionResult RegisterBusiness()
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterBusiness(RegisterBusinessViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
                ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
                return View(model);
            }

            if (!model.ConsentAccepted)
            {
                ModelState.AddModelError(nameof(model.ConsentAccepted), "Kullanım şartlarını kabul etmeniz gerekmektedir.");
                ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
                ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
                return View(model);
            }

            // Kullanıcı oluştur
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true,
                FullName = model.BusinessName + " Sahibi",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                ConsentAccepted = model.ConsentAccepted,
                ConsentDate = DateTime.UtcNow
            };

            var userResult = await _userManager.CreateAsync(user, model.Password);
            if (!userResult.Succeeded)
            {
                foreach (var error in userResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Categories = new SelectList(_context.Categories.ToList(), "Id", "Name");
                ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
                return View(model);
            }

            // BusinessOwner rolüne ekle
            var roleManager = HttpContext.RequestServices.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("BusinessOwner"))
            {
                await roleManager.CreateAsync(new IdentityRole("BusinessOwner"));
            }

            await _userManager.AddToRoleAsync(user, "BusinessOwner");

            // İşletme oluştur
            var business = new Business
            {
                Name = model.BusinessName,
                Address = model.Address,
                Phone = model.Phone,
                Email = model.BusinessEmail ?? model.Email,
                Website = model.Website,
                CategoryId = model.CategoryId,
                Description = model.Description,
                WorkingHours = model.WorkingHours,
                PriceRange = model.PriceRange,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                OwnerId = user.Id,
                CreatedBy = user.Id,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsApproved = false // Admin onayı bekliyor
            };

            // Resim yükleme
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "businesses");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                business.ImageUrl = $"/uploads/businesses/{fileName}";
            }

            _context.Businesses.Add(business);
            await _context.SaveChangesAsync();

            // Otomatik giriş yap
            await _signInManager.SignInAsync(user, isPersistent: false);

            TempData["Success"] = "İşletmeniz başarıyla kaydedildi! Admin onayından sonra yayınlanacaktır.";
            return RedirectToAction("Index", "BusinessOwner");
        }

        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var businesses = await _context.Businesses
                .Where(b => b.OwnerId == user.Id)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .ToListAsync();

            // Dashboard istatistikleri
            var allReservations = await _context.Reservations
                .Where(r => businesses.Select(b => b.Id).Contains(r.BusinessId))
                .ToListAsync();

            var stats = new
            {
                TotalBusinesses = businesses.Count,
                ActiveBusinesses = businesses.Count(b => b.IsActive),
                PendingApprovals = businesses.Count(b => !b.IsApproved),
                TotalReservations = allReservations.Count,
                TotalReviews = businesses.Sum(b => b.Reviews?.Count ?? 0),
                AverageRating = businesses.Any() ? businesses.Average(b => b.AverageRating) : 0
            };

            ViewBag.Stats = stats;
            return View(businesses);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            return View(business);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Business model, IFormFile? imageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            business.Name = model.Name;
            business.Address = model.Address;
            business.Phone = model.Phone;
            business.Description = model.Description;
            business.Email = model.Email;
            business.Website = model.Website;
            business.WorkingHours = model.WorkingHours;
            business.PriceRange = model.PriceRange;
            business.Latitude = model.Latitude;
            business.Longitude = model.Longitude;
            business.UpdatedDate = DateTime.Now;

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "businesses");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                business.ImageUrl = $"/uploads/businesses/{fileName}";
            }

            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            TempData["Success"] = "İşletme bilgileriniz güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Reservations(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == id)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            ViewBag.Business = business;
            return View(reservations);
        }

        // Rezervasyon Onaylama
        [HttpPost]
        public async Task<IActionResult> ApproveReservation(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return NotFound();
            }

            reservation.Status = "Onaylandı";
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezervasyon başarıyla onaylandı.";
            return RedirectToAction("Reservations", new { id = reservation.BusinessId });
        }

        // Rezervasyon Reddetme
        [HttpPost]
        public async Task<IActionResult> RejectReservation(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return NotFound();
            }

            reservation.Status = "Reddedildi";
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Rezervasyon reddedildi.";
            return RedirectToAction("Reservations", new { id = reservation.BusinessId });
        }

        // İşletme Favorileri
        public async Task<IActionResult> Favorites(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var favorites = await _context.UserFavorites
                .Where(f => f.BusinessId == id)
                .Include(f => f.User)
                .OrderByDescending(f => f.AddedDate)
                .ToListAsync();

            ViewBag.Business = business;
            return View(favorites);
        }

        // İşletme Ekleme
        public IActionResult Create()
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Business business, IFormFile? imageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                business.OwnerId = user.Id;
                business.CreatedBy = user.Id;
                business.CreatedDate = DateTime.Now;
                business.IsActive = true;
                business.IsApproved = false; // Admin onayı bekliyor

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "businesses");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(imageFile.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    business.ImageUrl = $"/uploads/businesses/{fileName}";
                }

                _context.Businesses.Add(business);
                await _context.SaveChangesAsync();

                TempData["Success"] = "İşletmeniz başarıyla eklendi. Admin onayından sonra yayınlanacaktır.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.GoogleMapsApiKey = _mapsOptions.ApiKey;
            return View(business);
        }
    }
}


