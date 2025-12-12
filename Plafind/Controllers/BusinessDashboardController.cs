using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using Plafind.ViewModels;
using System.Security.Claims;

namespace Plafind.Controllers
{
    [Authorize(Roles = "Admin,BusinessOwner")]
    public class BusinessDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BusinessDashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: BusinessDashboard
        public async Task<IActionResult> Index(int? businessId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FindAsync(userId);
            
            if (user == null)
            {
                return Unauthorized();
            }

            Business? business = null;

            // Admin ise tüm işletmeleri görebilir, BusinessOwner ise sadece kendi işletmelerini
            if (User.IsInRole("Admin"))
            {
                if (businessId.HasValue)
                {
                    business = await _context.Businesses
                        .Include(b => b.Category)
                        .FirstOrDefaultAsync(b => b.Id == businessId.Value);
                }
                else
                {
                    // İlk işletmeyi seç
                    business = await _context.Businesses
                        .Include(b => b.Category)
                        .FirstOrDefaultAsync();
                }
            }
            else
            {
                // BusinessOwner ise kendi işletmelerinden birini seç
                if (businessId.HasValue)
                {
                    business = await _context.Businesses
                        .Include(b => b.Category)
                        .FirstOrDefaultAsync(b => b.Id == businessId.Value && b.OwnerId == userId);
                }
                else
                {
                    business = await _context.Businesses
                        .Include(b => b.Category)
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı veya bu işletmeye erişim yetkiniz yok.");
            }

            var viewModel = await BuildDashboardViewModel(business);
            
            // Kullanıcının sahip olduğu tüm işletmeleri getir (çoklu işletme yönetimi için)
            var userBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();
            
            ViewBag.UserBusinesses = userBusinesses;
            ViewBag.CurrentBusinessId = business.Id;

            return View(viewModel);
        }

        private async Task<BusinessDashboardViewModel> BuildDashboardViewModel(Business business)
        {
            var viewModel = new BusinessDashboardViewModel
            {
                Business = business
            };

            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            // Rezervasyon İstatistikleri
            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == business.Id)
                .ToListAsync();

            viewModel.TotalReservations = reservations.Count;
            viewModel.PendingReservations = reservations.Count(r => r.Status == "Beklemede");
            viewModel.ConfirmedReservations = reservations.Count(r => r.Status == "Onaylandı" || r.Status == "Tamamlandı");
            viewModel.CancelledReservations = reservations.Count(r => r.Status == "İptal");

            // Yorum İstatistikleri
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == business.Id && r.IsActive && r.IsApproved)
                .ToListAsync();

            viewModel.TotalReviews = reviews.Count;
            viewModel.AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            viewModel.PositiveReviews = reviews.Count(r => r.Rating >= 4);
            viewModel.NegativeReviews = reviews.Count(r => r.Rating <= 2);

            // Müşteri İstatistikleri
            var customerIds = reservations
                .Where(r => !string.IsNullOrEmpty(r.UserId))
                .Select(r => r.UserId!)
                .Distinct()
                .ToList();

            viewModel.TotalCustomers = customerIds.Count;
            viewModel.NewCustomersThisMonth = reservations
                .Where(r => !string.IsNullOrEmpty(r.UserId) && r.CreatedDate >= startOfMonth)
                .Select(r => r.UserId!)
                .Distinct()
                .Count();

            var returningCustomerIds = reservations
                .Where(r => !string.IsNullOrEmpty(r.UserId) && r.CreatedDate < startOfMonth)
                .Select(r => r.UserId!)
                .Distinct()
                .ToList();
            
            var thisMonthCustomerIds = reservations
                .Where(r => !string.IsNullOrEmpty(r.UserId) && r.CreatedDate >= startOfMonth)
                .Select(r => r.UserId!)
                .Distinct()
                .ToList();
            
            viewModel.ReturningCustomers = returningCustomerIds.Intersect(thisMonthCustomerIds).Count();

            // Gelir İstatistikleri
            viewModel.TotalRevenue = reservations
                .Where(r => r.Amount.HasValue)
                .Sum(r => r.Amount ?? 0);

            viewModel.RevenueThisMonth = reservations
                .Where(r => r.Amount.HasValue && r.CreatedDate >= startOfMonth)
                .Sum(r => r.Amount ?? 0);

            viewModel.RevenueLastMonth = reservations
                .Where(r => r.Amount.HasValue && r.CreatedDate >= startOfLastMonth && r.CreatedDate < startOfMonth)
                .Sum(r => r.Amount ?? 0);

            viewModel.RevenueThisYear = reservations
                .Where(r => r.Amount.HasValue && r.CreatedDate >= startOfYear)
                .Sum(r => r.Amount ?? 0);

            // Aylık Gelir Grafiği (Son 12 ay)
            var monthlyRevenues = new List<MonthlyRevenue>();
            for (int i = 11; i >= 0; i--)
            {
                var monthStart = now.AddMonths(-i);
                var monthEnd = monthStart.AddMonths(1);
                var monthReservations = reservations
                    .Where(r => r.CreatedDate >= monthStart && r.CreatedDate < monthEnd && r.Amount.HasValue)
                    .ToList();

                monthlyRevenues.Add(new MonthlyRevenue
                {
                    Month = monthStart.ToString("MMM yyyy"),
                    Revenue = monthReservations.Sum(r => r.Amount ?? 0),
                    ReservationCount = monthReservations.Count
                });
            }
            viewModel.MonthlyRevenues = monthlyRevenues;

            // Günlük Rezervasyon Grafiği (Son 30 gün)
            var dailyReservations = new List<DailyReservation>();
            for (int i = 29; i >= 0; i--)
            {
                var date = now.AddDays(-i).Date;
                var count = reservations.Count(r => r.CreatedDate.Date == date);
                dailyReservations.Add(new DailyReservation
                {
                    Date = date.ToString("dd MMM"),
                    Count = count
                });
            }
            viewModel.DailyReservations = dailyReservations;

            // Rating Dağılımı
            var ratingGroups = reviews
                .GroupBy(r => r.Rating)
                .Select(g => new RatingDistribution
                {
                    Rating = g.Key,
                    Count = g.Count(),
                    Percentage = reviews.Any() ? (double)g.Count() / reviews.Count * 100 : 0
                })
                .OrderBy(r => r.Rating)
                .ToList();

            // Eksik rating'leri doldur
            for (int i = 1; i <= 5; i++)
            {
                if (!ratingGroups.Any(r => r.Rating == i))
                {
                    ratingGroups.Add(new RatingDistribution
                    {
                        Rating = i,
                        Count = 0,
                        Percentage = 0
                    });
                }
            }
            viewModel.RatingDistributions = ratingGroups.OrderBy(r => r.Rating).ToList();

            // Şube İstatistikleri
            var branches = await _context.Branches
                .Where(b => b.BusinessId == business.Id && b.IsActive)
                .ToListAsync();

            viewModel.TotalBranches = branches.Count;
            
            var branchStats = new List<BranchStats>();
            foreach (var branch in branches)
            {
                var branchReservations = reservations.Where(r => r.BranchId == branch.Id).ToList();
                var branchReviews = await _context.Reviews
                    .Where(r => r.BusinessId == business.Id && r.IsActive && r.IsApproved)
                    .ToListAsync(); // BranchId yoksa tüm yorumları al

                branchStats.Add(new BranchStats
                {
                    BranchId = branch.Id,
                    BranchName = branch.Name,
                    ReservationCount = branchReservations.Count,
                    Revenue = branchReservations.Where(r => r.Amount.HasValue).Sum(r => r.Amount ?? 0),
                    AverageRating = branchReviews.Any() ? branchReviews.Average(r => r.Rating) : 0
                });
            }
            viewModel.BranchStatistics = branchStats;

            // Son Aktiviteler
            viewModel.RecentReservations = reservations
                .OrderByDescending(r => r.CreatedDate)
                .Take(10)
                .ToList();

            viewModel.RecentReviews = reviews
                .OrderByDescending(r => r.CreatedDate)
                .Take(10)
                .ToList();

            return viewModel;
        }

        // API: Grafik verileri için JSON endpoint
        [HttpGet]
        public async Task<IActionResult> GetChartData(int businessId, string chartType)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var business = await _context.Businesses.FindAsync(businessId);

            if (business == null || (!User.IsInRole("Admin") && business.OwnerId != userId))
            {
                return Unauthorized();
            }

            switch (chartType.ToLower())
            {
                case "monthlyrevenue":
                    var reservations = await _context.Reservations
                        .Where(r => r.BusinessId == businessId && r.Amount.HasValue)
                        .ToListAsync();
                    
                    var monthlyData = new List<MonthlyRevenue>();
                    var now = DateTime.Now;
                    for (int i = 11; i >= 0; i--)
                    {
                        var monthStart = now.AddMonths(-i);
                        var monthEnd = monthStart.AddMonths(1);
                        var monthReservations = reservations
                            .Where(r => r.CreatedDate >= monthStart && r.CreatedDate < monthEnd)
                            .ToList();

                        monthlyData.Add(new MonthlyRevenue
                        {
                            Month = monthStart.ToString("MMM yyyy"),
                            Revenue = monthReservations.Sum(r => r.Amount ?? 0),
                            ReservationCount = monthReservations.Count
                        });
                    }
                    return Json(monthlyData);

                case "dailyreservations":
                    var allReservations = await _context.Reservations
                        .Where(r => r.BusinessId == businessId)
                        .ToListAsync();
                    
                    var dailyData = new List<DailyReservation>();
                    for (int i = 29; i >= 0; i--)
                    {
                        var date = DateTime.Now.AddDays(-i).Date;
                        dailyData.Add(new DailyReservation
                        {
                            Date = date.ToString("dd MMM"),
                            Count = allReservations.Count(r => r.CreatedDate.Date == date)
                        });
                    }
                    return Json(dailyData);

                default:
                    return BadRequest("Geçersiz chart tipi");
            }
        }
    }
}
