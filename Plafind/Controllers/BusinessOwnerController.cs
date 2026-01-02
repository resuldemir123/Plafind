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
using Plafind.Services;
using Microsoft.AspNetCore.Hosting;

namespace Plafind.Controllers
{
    public class AnalyzeReviewsRequest
    {
        public int Id { get; set; }
        public int? MaxReviews { get; set; } = 50;
    }

    public class BusinessOwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly GoogleMapsOptions _mapsOptions;
        private readonly TomTomOptions _tomTomOptions;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISentimentAnalysisService _sentimentAnalysisService;

        public BusinessOwnerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IWebHostEnvironment environment,
            IOptions<GoogleMapsOptions> mapsOptions,
            IOptions<TomTomOptions> tomTomOptions,
            ISentimentAnalysisService sentimentAnalysisService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _signInManager = signInManager ?? throw new ArgumentNullException(nameof(signInManager));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _mapsOptions = mapsOptions?.Value ?? new GoogleMapsOptions();
            _tomTomOptions = tomTomOptions?.Value ?? new TomTomOptions();
            _sentimentAnalysisService = sentimentAnalysisService ?? throw new ArgumentNullException(nameof(sentimentAnalysisService));
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
            ViewBag.TomTomApiKey = _tomTomOptions.ApiKey;
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
        public async Task<IActionResult> Index(int? businessId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var businesses = await _context.Businesses
                .Where(b => b.OwnerId == user.Id)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .ToListAsync();

            // Eğer businessId belirtilmişse, o işletmeye odaklan
            Business? selectedBusiness = null;
            if (businessId.HasValue)
            {
                selectedBusiness = businesses.FirstOrDefault(b => b.Id == businessId.Value);
            }
            else if (businesses.Any())
            {
                selectedBusiness = businesses.First();
            }

            if (selectedBusiness == null && businesses.Any())
            {
                selectedBusiness = businesses.First();
            }

            // Dashboard istatistikleri - Seçili işletme için
            var businessIds = selectedBusiness != null 
                ? new[] { selectedBusiness.Id } 
                : businesses.Select(b => b.Id).ToArray();

            var allReservations = await _context.Reservations
                .Where(r => businessIds.Contains(r.BusinessId))
                .ToListAsync();

            var fourteenDaysAgo = DateTime.Now.AddDays(-14);
            var recentReservations = allReservations
                .Where(r => r.CreatedDate >= fourteenDaysAgo)
                .ToList();

            // 14 günlük trend (günlük rezervasyon sayıları)
            var reservationTrend = new List<object>();
            for (int i = 13; i >= 0; i--)
            {
                var date = DateTime.Now.AddDays(-i).Date;
                var count = recentReservations.Count(r => r.CreatedDate.Date == date);
                reservationTrend.Add(new { Date = date.ToString("yyyy-MM-dd"), Count = count });
            }

            var cancelledReservations = allReservations.Count(r => r.Status == "İptal Edildi" || r.Status == "Reddedildi");
            var cancellationRate = allReservations.Any() 
                ? (double)cancelledReservations / allReservations.Count * 100 
                : 0;

            var stats = new
            {
                TotalBusinesses = businesses.Count,
                ActiveBusinesses = businesses.Count(b => b.IsActive),
                PendingApprovals = businesses.Count(b => !b.IsApproved),
                TotalReservations = allReservations.Count,
                TotalReviews = selectedBusiness != null 
                    ? (selectedBusiness.Reviews?.Count(r => r.IsActive && r.IsApproved) ?? 0)
                    : businesses.Sum(b => b.Reviews?.Count(r => r.IsActive && r.IsApproved) ?? 0),
                AverageRating = selectedBusiness != null 
                    ? selectedBusiness.AverageRating 
                    : (businesses.Any() ? businesses.Average(b => b.AverageRating) : 0),
                CancellationRate = Math.Round(cancellationRate, 2),
                ReservationTrend = reservationTrend
            };

            ViewBag.Stats = stats;
            ViewBag.SelectedBusiness = selectedBusiness;
            ViewBag.UserId = user.Id; // SignalR için kullanıcı ID'si
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

        // İşletme Yorumları
        public async Task<IActionResult> Reviews(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == id && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == id && r.IsActive)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            ViewBag.Business = business;
            return View(reviews);
        }

        // Yorum Analizi (AJAX)
        [HttpPost]
        public async Task<IActionResult> AnalyzeReviews([FromBody] AnalyzeReviewsRequest request)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            if (request == null || request.Id <= 0)
            {
                return Json(new { success = false, message = "Geçersiz istek" });
            }

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.Id && b.OwnerId == user.Id);

            if (business == null) return Json(new { success = false, message = "İşletme bulunamadı" });

            try
            {
                var result = await _sentimentAnalysisService.AnalyzeBusinessReviewsAsync(request.Id, request.MaxReviews ?? 50);
                return Json(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Analiz yapılırken hata oluştu: {ex.Message}" });
            }
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

        // ==================== GELİŞMİŞ REZERVASYON YÖNETİMİ ====================
        
        /// <summary>
        /// Gelişmiş rezervasyon listesi (filtrelerle)
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> ReservationsAdvanced(int businessId, 
            string? status, 
            DateTime? startDate, 
            DateTime? endDate,
            string? channel,
            string? package,
            string? customerName,
            int page = 1,
            int pageSize = 20)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var query = _context.Reservations
                .Where(r => r.BusinessId == businessId)
                .Include(r => r.User)
                .Include(r => r.Customer)
                .Include(r => r.Branch)
                .AsQueryable();

            // Filtreler
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(r => r.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.RequestedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.RequestedDate <= endDate.Value);
            }

            if (!string.IsNullOrEmpty(channel))
            {
                query = query.Where(r => r.Channel == channel);
            }

            if (!string.IsNullOrEmpty(package))
            {
                query = query.Where(r => r.PackageName == package);
            }

            if (!string.IsNullOrEmpty(customerName))
            {
                query = query.Where(r => 
                    (r.User != null && r.User.FullName != null && r.User.FullName.Contains(customerName)) ||
                    (r.Customer != null && r.Customer.FullName.Contains(customerName)) ||
                    (r.ContactPhone != null && r.ContactPhone.Contains(customerName)) ||
                    (r.ContactEmail != null && r.ContactEmail.Contains(customerName)));
            }

            var totalCount = await query.CountAsync();
            var reservations = await query
                .OrderByDescending(r => r.RequestedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.Status = status;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.Channel = channel;
            ViewBag.Package = package;
            ViewBag.CustomerName = customerName;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Filtre seçenekleri
            ViewBag.Statuses = new[] { "Beklemede", "Onaylandı", "İptal Edildi", "Reddedildi", "Tamamlandı", "No-Show" };
            ViewBag.Channels = await _context.Reservations
                .Where(r => r.BusinessId == businessId && r.Channel != null)
                .Select(r => r.Channel)
                .Distinct()
                .ToListAsync();
            ViewBag.Packages = await _context.Reservations
                .Where(r => r.BusinessId == businessId && r.PackageName != null)
                .Select(r => r.PackageName)
                .Distinct()
                .ToListAsync();

            return View(reservations);
        }

        /// <summary>
        /// Rezervasyon detay sayfası
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> ReservationDetails(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .Include(r => r.User)
                .Include(r => r.Customer)
                .Include(r => r.Branch)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return NotFound();
            }

            return View(reservation);
        }

        /// <summary>
        /// Rezervasyon onaylama
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> ApproveReservationAdvanced(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.Status = "Onaylandı";
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rezervasyon onaylandı" });
        }

        /// <summary>
        /// Rezervasyon iptal etme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CancelReservationAdvanced(int id, string? reason)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.Status = "İptal Edildi";
            reservation.UpdatedDate = DateTime.UtcNow;
            if (!string.IsNullOrEmpty(reason))
            {
                reservation.OwnerNotes = (reservation.OwnerNotes ?? "") + $"\n[İptal Nedeni: {reason}]";
            }
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rezervasyon iptal edildi" });
        }

        /// <summary>
        /// Rezervasyon tarih değiştirme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdateReservationDate(int id, DateTime newDate, TimeSpan? newTime)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.RequestedDate = newDate;
            if (newTime.HasValue)
            {
                reservation.RequestedTime = newTime.Value;
            }
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rezervasyon tarihi güncellendi" });
        }

        /// <summary>
        /// Kişi sayısı güncelleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdateReservationPeople(int id, int numberOfPeople)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.NumberOfPeople = numberOfPeople;
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Kişi sayısı güncellendi" });
        }

        /// <summary>
        /// No-show işaretleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> MarkNoShow(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.IsNoShow = true;
            reservation.NoShowDate = DateTime.UtcNow;
            reservation.Status = "No-Show";
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Rezervasyon No-Show olarak işaretlendi" });
        }

        /// <summary>
        /// Rezervasyon notu ekleme/güncelleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdateReservationNotes(int id, string notes, string? tags)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.OwnerNotes = notes;
            if (!string.IsNullOrEmpty(tags))
            {
                reservation.Tags = tags;
            }
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Notlar güncellendi" });
        }

        /// <summary>
        /// Ödeme durumu güncelleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, decimal? prePaymentAmount, decimal? remainingAmount, bool isPrePaymentReceived, bool isFullPaymentReceived)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.PrePaymentAmount = prePaymentAmount;
            reservation.RemainingAmount = remainingAmount;
            reservation.IsPrePaymentReceived = isPrePaymentReceived;
            reservation.IsFullPaymentReceived = isFullPaymentReceived;
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ödeme durumu güncellendi" });
        }

        /// <summary>
        /// Check-in/out bilgileri güncelleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdateCheckInOut(int id, DateTime? checkInDate, TimeSpan? checkInTime, DateTime? checkOutDate, TimeSpan? checkOutTime, DateTime? tourStartTime, DateTime? tourEndTime)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var reservation = await _context.Reservations
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null || reservation.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Rezervasyon bulunamadı" });
            }

            reservation.CheckInDate = checkInDate;
            reservation.CheckInTime = checkInTime;
            reservation.CheckOutDate = checkOutDate;
            reservation.CheckOutTime = checkOutTime;
            reservation.TourStartTime = tourStartTime;
            reservation.TourEndTime = tourEndTime;
            reservation.UpdatedDate = DateTime.UtcNow;
            _context.Reservations.Update(reservation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Check-in/out bilgileri güncellendi" });
        }

        /// <summary>
        /// Takvim görünümü
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> ReservationsCalendar(int businessId, int? year, int? month)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var targetDate = new DateTime(year ?? DateTime.Now.Year, month ?? DateTime.Now.Month, 1);
            var startDate = targetDate;
            var endDate = targetDate.AddMonths(1).AddDays(-1);

            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == businessId && 
                           r.RequestedDate >= startDate && 
                           r.RequestedDate <= endDate)
                .Include(r => r.User)
                .Include(r => r.Customer)
                .OrderBy(r => r.RequestedDate)
                .ThenBy(r => r.RequestedTime)
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.Year = targetDate.Year;
            ViewBag.Month = targetDate.Month;
            ViewBag.Reservations = reservations;

            return View();
        }

        // ==================== MÜŞTERİ YÖNETİMİ (CRM) ====================
        
        /// <summary>
        /// Müşteri listesi
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> Customers(int businessId, string? segment, string? tag, string? search, int page = 1, int pageSize = 20)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var query = _context.Customers
                .Where(c => c.BusinessId == businessId)
                .Include(c => c.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(segment))
            {
                query = query.Where(c => c.Segment == segment);
            }

            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(c => c.Tags != null && c.Tags.Contains(tag));
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => 
                    c.FullName.Contains(search) ||
                    (c.Email != null && c.Email.Contains(search)) ||
                    (c.Phone != null && c.Phone.Contains(search)));
            }

            var totalCount = await query.CountAsync();
            var customers = await query
                .OrderByDescending(c => c.LastVisitDate ?? c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Business = business;
            ViewBag.Segment = segment;
            ViewBag.Tag = tag;
            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            ViewBag.Segments = new[] { "Aile", "Çift", "İş Seyahati", "Yabancı Turist", "Grup", "Tekil" };
            ViewBag.Tags = new[] { "VIP", "Sorunlu", "Tekrar Gelen", "Yeni Müşteri" };

            return View(customers);
        }

        /// <summary>
        /// Müşteri detay sayfası
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CustomerDetails(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers
                .Include(c => c.Business)
                .Include(c => c.User)
                .Include(c => c.Reservations)
                    .ThenInclude(r => r.Business)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null || customer.Business?.OwnerId != user.Id)
            {
                return NotFound();
            }

            return View(customer);
        }

        /// <summary>
        /// Müşteri notu ekleme/güncelleme
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> UpdateCustomerNotes(int id, string notes, string? tags, string? segment, bool? isVIP, bool? hasIssues)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null || customer.Business?.OwnerId != user.Id)
            {
                return Json(new { success = false, message = "Müşteri bulunamadı" });
            }

            customer.Notes = notes;
            if (!string.IsNullOrEmpty(tags))
            {
                customer.Tags = tags;
            }
            if (!string.IsNullOrEmpty(segment))
            {
                customer.Segment = segment;
            }
            if (isVIP.HasValue)
            {
                customer.IsVIP = isVIP.Value;
            }
            if (hasIssues.HasValue)
            {
                customer.HasIssues = hasIssues.Value;
            }
            customer.UpdatedDate = DateTime.UtcNow;
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Müşteri bilgileri güncellendi" });
        }

        // ==================== FİYATLANDIRMA YÖNETİMİ ====================
        
        /// <summary>
        /// Fiyatlandırma listesi
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> Pricings(int businessId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var pricings = await _context.Pricings
                .Where(p => p.BusinessId == businessId)
                .OrderBy(p => p.StartDate)
                .ToListAsync();

            ViewBag.Business = business;
            return View(pricings);
        }

        /// <summary>
        /// Fiyatlandırma oluşturma/düzenleme
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> PricingForm(int businessId, int? id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            Pricing? pricing = null;
            if (id.HasValue)
            {
                pricing = await _context.Pricings
                    .FirstOrDefaultAsync(p => p.Id == id.Value && p.BusinessId == businessId);
                if (pricing == null) return NotFound();
            }

            ViewBag.Business = business;
            ViewBag.PricingTypes = new[] { "PerPerson", "PerNight", "PerHour", "Fixed" };
            return View(pricing);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> PricingForm(Pricing pricing)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == pricing.BusinessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (pricing.Id == 0)
                {
                    pricing.CreatedDate = DateTime.UtcNow;
                    _context.Pricings.Add(pricing);
                }
                else
                {
                    pricing.UpdatedDate = DateTime.UtcNow;
                    _context.Pricings.Update(pricing);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Fiyatlandırma kaydedildi";
                return RedirectToAction("Pricings", new { businessId = pricing.BusinessId });
            }

            ViewBag.Business = business;
            ViewBag.PricingTypes = new[] { "PerPerson", "PerNight", "PerHour", "Fixed" };
            return View(pricing);
        }

        // ==================== KAMPANYA YÖNETİMİ ====================
        
        /// <summary>
        /// Kampanya listesi ve performans
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> Campaigns(int businessId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            var campaigns = await _context.Campaigns
                .Where(c => c.BusinessId == businessId)
                .Include(c => c.Usages)
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();

            // Performans hesaplamaları
            foreach (var campaign in campaigns)
            {
                campaign.CurrentUses = campaign.Usages.Count;
                campaign.TotalRevenueImpact = campaign.Usages.Sum(u => u.DiscountApplied ?? 0);
                campaign.AverageDiscountApplied = campaign.Usages.Any() 
                    ? campaign.Usages.Average(u => u.DiscountApplied ?? 0) 
                    : 0;
            }

            ViewBag.Business = business;
            return View(campaigns);
        }

        /// <summary>
        /// Kampanya oluşturma/düzenleme
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CampaignForm(int businessId, int? id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == businessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            Campaign? campaign = null;
            if (id.HasValue)
            {
                campaign = await _context.Campaigns
                    .FirstOrDefaultAsync(c => c.Id == id.Value && c.BusinessId == businessId);
                if (campaign == null) return NotFound();
            }

            // İşletme kategorisine göre kampanya şablonları
            var categoryName = business.Category?.Name ?? "";
            var campaignTemplates = GetCampaignTemplatesByCategory(categoryName);
            
            ViewBag.Business = business;
            ViewBag.CategoryName = categoryName;
            ViewBag.CampaignTypes = new[] { "Discount", "Promotion", "SpecialOffer", "Package" };
            ViewBag.PackageTypes = new[] { "Stay3Pay2", "EarlyBooking", "LastMinute", "GroupDiscount" };
            ViewBag.CampaignTemplates = campaignTemplates;
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CampaignForm(Campaign campaign, IFormFile? imageFile)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == campaign.BusinessId && b.OwnerId == user.Id);

            if (business == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Görsel yükleme
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "campaigns");
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

                    campaign.ImageUrl = $"/uploads/campaigns/{fileName}";
                }

                // IsVisibleToCustomers default true olmalı (yeni kampanyalar için)
                if (campaign.Id == 0)
                {
                    campaign.CreatedDate = DateTime.UtcNow;
                    campaign.CreatedBy = user.Id;
                    // Yeni kampanya oluşturulurken müşterilere görünür olmalı (default true)
                    campaign.IsVisibleToCustomers = true;
                    _context.Campaigns.Add(campaign);
                }
                else
                {
                    campaign.UpdatedDate = DateTime.UtcNow;
                    _context.Campaigns.Update(campaign);
                }
                await _context.SaveChangesAsync();
                TempData["Success"] = "Kampanya kaydedildi";
                return RedirectToAction("Campaigns", new { businessId = campaign.BusinessId });
            }

            ViewBag.Business = business;
            ViewBag.CampaignTypes = new[] { "Discount", "Promotion", "SpecialOffer", "Package" };
            ViewBag.PackageTypes = new[] { "Stay3Pay2", "EarlyBooking", "LastMinute", "GroupDiscount" };
            return View(campaign);
        }

        /// <summary>
        /// Kampanya performans detayı
        /// </summary>
        [Authorize(Roles = "BusinessOwner")]
        public async Task<IActionResult> CampaignPerformance(int id)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToAction("Login", "Account");

            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .Include(c => c.Usages)
                    .ThenInclude(u => u.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null || campaign.Business?.OwnerId != user.Id)
            {
                return NotFound();
            }

            var performance = new
            {
                Campaign = campaign,
                TotalUses = campaign.Usages.Count,
                TotalRevenueImpact = campaign.Usages.Sum(u => u.DiscountApplied ?? 0),
                AverageDiscount = campaign.Usages.Any() 
                    ? campaign.Usages.Average(u => u.DiscountApplied ?? 0) 
                    : 0,
                UsageByDate = campaign.Usages
                    .GroupBy(u => u.UsedDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToList()
            };

            ViewBag.Performance = performance;
            return View(campaign);
        }

        /// <summary>
        /// İşletme kategorisine göre kampanya şablonlarını döndürür
        /// </summary>
        private Dictionary<string, List<CampaignTemplate>> GetCampaignTemplatesByCategory(string categoryName)
        {
            var templates = new Dictionary<string, List<CampaignTemplate>>();

            switch (categoryName.ToLower())
            {
                case "otel":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Erken Rezervasyon İndirimi", Description = "Erken rezervasyon yapan misafirlere özel indirim", DiscountPercentage = 15, PackageType = "EarlyBooking" },
                        new CampaignTemplate { Title = "Son Dakika Fırsatı", Description = "Son dakika rezervasyonlarında özel fiyat", DiscountPercentage = 20, PackageType = "LastMinute" },
                        new CampaignTemplate { Title = "3 Gece Kal 2 Öde", Description = "3 gece konaklama yapan misafirlere 1 gece bedava", PackageType = "Stay3Pay2", StayNights = 3, PayNights = 2 },
                        new CampaignTemplate { Title = "Hafta Sonu Paketi", Description = "Cuma-Pazar arası özel paket fiyatı", DiscountPercentage = 10 },
                        new CampaignTemplate { Title = "Uzun Konaklama İndirimi", Description = "7 gece ve üzeri konaklamalarda özel indirim", DiscountPercentage = 25, MinimumPeople = 1 },
                        new CampaignTemplate { Title = "Grup İndirimi", Description = "10 kişi ve üzeri gruplara özel fiyat", DiscountPercentage = 15, MinimumPeople = 10, PackageType = "GroupDiscount" },
                        new CampaignTemplate { Title = "Tatil Paketi", Description = "Konaklama + kahvaltı + aktivite paketi", DiscountPercentage = 20, IsPackageCampaign = true }
                    };
                    break;

                case "restoran":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Öğle Menüsü İndirimi", Description = "Öğle saatlerinde özel menü fiyatı", DiscountPercentage = 20 },
                        new CampaignTemplate { Title = "Hafta Sonu Brunch", Description = "Cumartesi-Pazar brunch menüsü", DiscountPercentage = 15 },
                        new CampaignTemplate { Title = "Doğum Günü Paketi", Description = "Doğum günü kutlamalarına özel paket", DiscountPercentage = 10, MinimumPeople = 4 },
                        new CampaignTemplate { Title = "İkinci Yemek %50 İndirimli", Description = "Aynı gün içinde ikinci yemekte yarı fiyat", DiscountPercentage = 50 },
                        new CampaignTemplate { Title = "Aile Menüsü", Description = "4 kişi ve üzeri ailelere özel menü", DiscountPercentage = 15, MinimumPeople = 4 },
                        new CampaignTemplate { Title = "Erken Akşam Yemeği", Description = "18:00-19:30 arası özel fiyat", DiscountPercentage = 25 },
                        new CampaignTemplate { Title = "Öğrenci İndirimi", Description = "Öğrencilere özel indirimli menü", DiscountPercentage = 20 }
                    };
                    break;

                case "mağaza":
                case "butik":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Sezon Sonu İndirimi", Description = "Sezon sonu ürünlerde büyük indirim", DiscountPercentage = 40 },
                        new CampaignTemplate { Title = "İkinci Ürün %50 İndirimli", Description = "İkinci üründe yarı fiyat", DiscountPercentage = 50 },
                        new CampaignTemplate { Title = "Toplu Alışveriş İndirimi", Description = "500 TL ve üzeri alışverişlerde ekstra indirim", DiscountPercentage = 15, MinimumPurchaseAmount = 500 },
                        new CampaignTemplate { Title = "Yeni Koleksiyon Ön İzleme", Description = "Yeni koleksiyon ön sipariş indirimi", DiscountPercentage = 20 },
                        new CampaignTemplate { Title = "VIP Müşteri İndirimi", Description = "VIP müşterilere özel indirim", DiscountPercentage = 25 },
                        new CampaignTemplate { Title = "Hediye Paketi", Description = "Belirli tutar üzeri alışverişlerde hediye", DiscountAmount = 100, MinimumPurchaseAmount = 1000 }
                    };
                    break;

                case "kafe":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Kahve + Pasta Paketi", Description = "Kahve ve pasta birlikte alımlarda indirim", DiscountPercentage = 15 },
                        new CampaignTemplate { Title = "Sabah Kahvaltı Paketi", Description = "Sabah saatlerinde özel kahvaltı menüsü", DiscountPercentage = 20 },
                        new CampaignTemplate { Title = "İkinci İçecek Bedava", Description = "Aynı gün içinde ikinci içecek ücretsiz", DiscountPercentage = 50 },
                        new CampaignTemplate { Title = "Öğrenci İndirimi", Description = "Öğrencilere özel fiyat", DiscountPercentage = 15 },
                        new CampaignTemplate { Title = "Toplu Sipariş İndirimi", Description = "10 adet ve üzeri siparişlerde indirim", DiscountPercentage = 20, MinimumPeople = 10 }
                    };
                    break;

                case "spa & wellness":
                case "spa":
                case "wellness":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Paket Seans İndirimi", Description = "3 seans ve üzeri paketlerde indirim", DiscountPercentage = 20, MinimumPeople = 3 },
                        new CampaignTemplate { Title = "Çift Paketi", Description = "Çiftler için özel paket fiyatı", DiscountPercentage = 15, MinimumPeople = 2 },
                        new CampaignTemplate { Title = "Hafta İçi Özel Fiyat", Description = "Pazartesi-Cuma arası özel fiyat", DiscountPercentage = 25 },
                        new CampaignTemplate { Title = "Yeni Müşteri İndirimi", Description = "İlk kez gelen müşterilere özel indirim", DiscountPercentage = 30 },
                        new CampaignTemplate { Title = "Doğum Günü Paketi", Description = "Doğum gününde özel paket", DiscountPercentage = 20 }
                    };
                    break;

                case "eğlence":
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Grup İndirimi", Description = "10 kişi ve üzeri gruplara özel fiyat", DiscountPercentage = 20, MinimumPeople = 10 },
                        new CampaignTemplate { Title = "Hafta İçi Özel Fiyat", Description = "Pazartesi-Perşembe arası özel fiyat", DiscountPercentage = 25 },
                        new CampaignTemplate { Title = "Doğum Günü Paketi", Description = "Doğum günü kutlamalarına özel paket", DiscountPercentage = 15, MinimumPeople = 5 },
                        new CampaignTemplate { Title = "Erken Rezervasyon İndirimi", Description = "Erken rezervasyon yapanlara özel indirim", DiscountPercentage = 20, PackageType = "EarlyBooking" }
                    };
                    break;

                default:
                    templates["Başlık Şablonları"] = new List<CampaignTemplate>
                    {
                        new CampaignTemplate { Title = "Genel İndirim", Description = "Tüm ürünlerde geçerli indirim", DiscountPercentage = 15 },
                        new CampaignTemplate { Title = "Yeni Müşteri İndirimi", Description = "İlk kez gelen müşterilere özel indirim", DiscountPercentage = 20 },
                        new CampaignTemplate { Title = "Grup İndirimi", Description = "Toplu alımlarda özel fiyat", DiscountPercentage = 15, MinimumPeople = 5 }
                    };
                    break;
            }

            return templates;
        }
    }

    /// <summary>
    /// Kampanya şablonu modeli
    /// </summary>
    public class CampaignTemplate
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? PackageType { get; set; }
        public int? StayNights { get; set; }
        public int? PayNights { get; set; }
        public int? MinimumPeople { get; set; }
        public decimal? MinimumPurchaseAmount { get; set; }
        public bool IsPackageCampaign { get; set; } = false;
    }
}


