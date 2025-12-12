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
    public class CustomerManagementController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerManagementController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: CustomerManagement - Müşteri listesi
        public async Task<IActionResult> Index(int? businessId, string? searchTerm, string? segmentFilter, string? sortBy, int page = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Business? business = null;
            
            if (businessId.HasValue)
            {
                business = await _context.Businesses.FindAsync(businessId.Value);
                
                if (business == null)
                {
                    return NotFound();
                }
                
                if (!User.IsInRole("Admin") && business.OwnerId != userId)
                {
                    return Forbid();
                }
            }
            else
            {
                if (User.IsInRole("Admin"))
                {
                    business = await _context.Businesses.FirstOrDefaultAsync();
                }
                else
                {
                    business = await _context.Businesses
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı.");
            }

            // Bu işletmeye rezervasyon yapan müşterileri bul
            var customerIds = await _context.Reservations
                .Where(r => r.BusinessId == business.Id && !string.IsNullOrEmpty(r.UserId))
                .Select(r => r.UserId!)
                .Distinct()
                .ToListAsync();

            var customers = await _context.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToListAsync();

            // Arama filtresi
            if (!string.IsNullOrEmpty(searchTerm))
            {
                customers = customers
                    .Where(c => 
                        (c.FullName != null && c.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (c.Email != null && c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (c.PhoneNumber != null && c.PhoneNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            // Müşteri bilgilerini ve istatistiklerini hazırla
            var customerInfos = new List<CustomerInfo>();
            
            foreach (var customer in customers)
            {
                var reservations = await _context.Reservations
                    .Where(r => r.BusinessId == business.Id && r.UserId == customer.Id)
                    .ToListAsync();

                var reviews = await _context.Reviews
                    .Where(r => r.BusinessId == business.Id && r.UserId == customer.Id && r.IsActive && r.IsApproved)
                    .ToListAsync();

                var last30Days = DateTime.Now.AddDays(-30);
                var visitFrequency = reservations.Count(r => r.CreatedDate >= last30Days);

                // Segmentasyon
                string segment = "New";
                if (reservations.Count >= 10 && visitFrequency >= 3)
                {
                    segment = "VIP";
                }
                else if (reservations.Count >= 5)
                {
                    segment = "Regular";
                }
                else if (reservations.Count > 0 && reservations.Max(r => r.CreatedDate) < last30Days)
                {
                    segment = "AtRisk";
                }

                // Segment filtresi
                if (!string.IsNullOrEmpty(segmentFilter) && segment != segmentFilter)
                {
                    continue;
                }

                var totalSpent = reservations
                    .Where(r => r.Amount.HasValue)
                    .Sum(r => r.Amount ?? 0);

                customerInfos.Add(new CustomerInfo
                {
                    UserId = customer.Id,
                    FullName = customer.FullName ?? customer.UserName ?? "İsimsiz",
                    Email = customer.Email,
                    Phone = customer.PhoneNumber,
                    AvatarUrl = customer.AvatarUrl,
                    TotalReservations = reservations.Count,
                    TotalReviews = reviews.Count,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    TotalSpent = totalSpent,
                    LastVisitDate = reservations.Any() ? reservations.Max(r => r.CreatedDate) : null,
                    FirstVisitDate = reservations.Any() ? reservations.Min(r => r.CreatedDate) : null,
                    CustomerSegment = segment,
                    VisitFrequency = visitFrequency,
                    LastReservation = reservations.OrderByDescending(r => r.CreatedDate).FirstOrDefault(),
                    LastReview = reviews.OrderByDescending(r => r.CreatedDate).FirstOrDefault()
                });
            }

            // Sıralama
            sortBy ??= "LastVisit";
            customerInfos = sortBy switch
            {
                "Name" => customerInfos.OrderBy(c => c.FullName).ToList(),
                "TotalSpent" => customerInfos.OrderByDescending(c => c.TotalSpent).ToList(),
                "Rating" => customerInfos.OrderByDescending(c => c.AverageRating).ToList(),
                _ => customerInfos.OrderByDescending(c => c.LastVisitDate ?? DateTime.MinValue).ToList()
            };

            // Sayfalama
            const int pageSize = 20;
            var totalCustomers = customerInfos.Count;
            var totalPages = (int)Math.Ceiling(totalCustomers / (double)pageSize);
            customerInfos = customerInfos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var viewModel = new CustomerListViewModel
            {
                BusinessId = business.Id,
                Business = business,
                Customers = customerInfos,
                SearchTerm = searchTerm,
                SegmentFilter = segmentFilter,
                SortBy = sortBy,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCustomers = totalCustomers
            };

            ViewBag.UserBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();

            return View(viewModel);
        }

        // GET: CustomerManagement/Details/5 - Müşteri detayı
        public async Task<IActionResult> Details(string customerId, int businessId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var business = await _context.Businesses.FindAsync(businessId);

            if (business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && business.OwnerId != userId)
            {
                return Forbid();
            }

            var customer = await _context.Users.FindAsync(customerId);
            if (customer == null)
            {
                return NotFound();
            }

            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == businessId && r.UserId == customerId)
                .Include(r => r.Branch)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && r.UserId == customerId && r.IsActive && r.IsApproved)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            var interactions = await _context.CustomerInteractions
                .Where(ci => ci.BusinessId == businessId && ci.CustomerId == customerId)
                .OrderByDescending(ci => ci.InteractionDate)
                .ToListAsync();

            var messages = await _context.Messages
                .Where(m => (m.SenderId == customerId && m.RelatedBusinessId == businessId) ||
                           (m.ReceiverId == customerId && m.RelatedBusinessId == businessId))
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.CreatedDate)
                .ToListAsync();

            // İstatistikler
            var lifetimeValue = reservations
                .Where(r => r.Amount.HasValue)
                .Sum(r => r.Amount ?? 0);

            var totalVisits = reservations.Count;
            var averageSpending = totalVisits > 0 ? (double)(lifetimeValue / totalVisits) : 0.0;

            // En çok tercih edilen saat
            var preferredTime = reservations
                .GroupBy(r => r.RequestedTime.Hours)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key.ToString("00:00") ?? "Belirtilmemiş";

            var preferredPartySize = reservations.Any()
                ? (int)reservations.Average(r => r.NumberOfPeople)
                : 0;

            var customerInfo = new CustomerInfo
            {
                UserId = customer.Id,
                FullName = customer.FullName ?? customer.UserName ?? "İsimsiz",
                Email = customer.Email,
                Phone = customer.PhoneNumber,
                AvatarUrl = customer.AvatarUrl,
                TotalReservations = reservations.Count,
                TotalReviews = reviews.Count,
                AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                TotalSpent = lifetimeValue,
                LastVisitDate = reservations.Any() ? reservations.Max(r => r.CreatedDate) : null,
                FirstVisitDate = reservations.Any() ? reservations.Min(r => r.CreatedDate) : null
            };

            var viewModel = new CustomerDetailViewModel
            {
                Customer = customer,
                BusinessId = businessId,
                Business = business,
                CustomerInfo = customerInfo,
                Reservations = reservations,
                Reviews = reviews,
                Interactions = interactions,
                Messages = messages,
                LifetimeValue = lifetimeValue,
                TotalVisits = totalVisits,
                AverageSpending = averageSpending,
                PreferredTime = preferredTime,
                PreferredPartySize = preferredPartySize
            };

            return View(viewModel);
        }

        // GET: CustomerManagement/Segments - Müşteri segmentasyonu
        public async Task<IActionResult> Segments(int? businessId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Business? business = null;
            
            if (businessId.HasValue)
            {
                business = await _context.Businesses.FindAsync(businessId.Value);
                
                if (business == null || (!User.IsInRole("Admin") && business.OwnerId != userId))
                {
                    return NotFound();
                }
            }
            else
            {
                if (User.IsInRole("Admin"))
                {
                    business = await _context.Businesses.FirstOrDefaultAsync();
                }
                else
                {
                    business = await _context.Businesses
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı.");
            }

            var customerIds = await _context.Reservations
                .Where(r => r.BusinessId == business.Id && !string.IsNullOrEmpty(r.UserId))
                .Select(r => r.UserId!)
                .Distinct()
                .ToListAsync();

            var customers = await _context.Users
                .Where(u => customerIds.Contains(u.Id))
                .ToListAsync();

            var last30Days = DateTime.Now.AddDays(-30);
            
            var segments = new Dictionary<string, List<CustomerInfo>>
            {
                { "VIP", new List<CustomerInfo>() },
                { "Regular", new List<CustomerInfo>() },
                { "New", new List<CustomerInfo>() },
                { "AtRisk", new List<CustomerInfo>() }
            };

            foreach (var customer in customers)
            {
                var reservations = await _context.Reservations
                    .Where(r => r.BusinessId == business.Id && r.UserId == customer.Id)
                    .ToListAsync();

                var reviews = await _context.Reviews
                    .Where(r => r.BusinessId == business.Id && r.UserId == customer.Id && r.IsActive && r.IsApproved)
                    .ToListAsync();

                var visitFrequency = reservations.Count(r => r.CreatedDate >= last30Days);
                var totalSpent = reservations.Where(r => r.Amount.HasValue).Sum(r => r.Amount ?? 0);

                string segment = "New";
                if (reservations.Count >= 10 && visitFrequency >= 3)
                {
                    segment = "VIP";
                }
                else if (reservations.Count >= 5)
                {
                    segment = "Regular";
                }
                else if (reservations.Count > 0 && reservations.Max(r => r.CreatedDate) < last30Days)
                {
                    segment = "AtRisk";
                }

                var customerInfo = new CustomerInfo
                {
                    UserId = customer.Id,
                    FullName = customer.FullName ?? customer.UserName ?? "İsimsiz",
                    Email = customer.Email,
                    Phone = customer.PhoneNumber,
                    AvatarUrl = customer.AvatarUrl,
                    TotalReservations = reservations.Count,
                    TotalReviews = reviews.Count,
                    AverageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0,
                    TotalSpent = totalSpent,
                    LastVisitDate = reservations.Any() ? reservations.Max(r => r.CreatedDate) : null,
                    CustomerSegment = segment,
                    VisitFrequency = visitFrequency
                };

                segments[segment].Add(customerInfo);
            }

            ViewBag.Business = business;
            ViewBag.BusinessId = business.Id;
            ViewBag.Segments = segments;
            ViewBag.UserBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();

            return View();
        }

        // GET: CustomerManagement/Interactions/5 - Müşteri iletişim geçmişi
        public async Task<IActionResult> Interactions(int? businessId, string? customerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            Business? business = null;
            
            if (businessId.HasValue)
            {
                business = await _context.Businesses.FindAsync(businessId.Value);
                
                if (business == null || (!User.IsInRole("Admin") && business.OwnerId != userId))
                {
                    return NotFound();
                }
            }
            else
            {
                if (User.IsInRole("Admin"))
                {
                    business = await _context.Businesses.FirstOrDefaultAsync();
                }
                else
                {
                    business = await _context.Businesses
                        .FirstOrDefaultAsync(b => b.OwnerId == userId);
                }
            }

            if (business == null)
            {
                return NotFound("İşletme bulunamadı.");
            }

            IQueryable<CustomerInteraction> interactionsQuery = _context.CustomerInteractions
                .Where(ci => ci.BusinessId == business.Id)
                .Include(ci => ci.Customer)
                .Include(ci => ci.RelatedReservation)
                .Include(ci => ci.RelatedReview)
                .Include(ci => ci.RelatedMessage);

            if (!string.IsNullOrEmpty(customerId))
            {
                interactionsQuery = interactionsQuery.Where(ci => ci.CustomerId == customerId);
            }

            var interactions = await interactionsQuery
                .OrderByDescending(ci => ci.InteractionDate)
                .ToListAsync();

            // Eğer otomatik oluşturulmamışsa, mevcut rezervasyon, yorum ve mesajlardan oluştur
            if (!interactions.Any())
            {
                await CreateInteractionsFromExistingData(business.Id);
                interactions = await interactionsQuery
                    .OrderByDescending(ci => ci.InteractionDate)
                    .ToListAsync();
            }

            ViewBag.Business = business;
            ViewBag.BusinessId = business.Id;
            ViewBag.CustomerId = customerId;
            ViewBag.UserBusinesses = User.IsInRole("Admin")
                ? await _context.Businesses.ToListAsync()
                : await _context.Businesses.Where(b => b.OwnerId == userId).ToListAsync();

            return View(interactions);
        }

        // POST: CustomerManagement/Interactions/Create - Yeni iletişim kaydı oluştur
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateInteraction(CustomerInteraction interaction)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var business = await _context.Businesses.FindAsync(interaction.BusinessId);

            if (business == null || (!User.IsInRole("Admin") && business.OwnerId != userId))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                interaction.CreatedBy = userId;
                interaction.CreatedDate = DateTime.Now;
                _context.Add(interaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Interactions), new { businessId = interaction.BusinessId, customerId = interaction.CustomerId });
            }

            return RedirectToAction(nameof(Interactions), new { businessId = interaction.BusinessId });
        }

        private async Task CreateInteractionsFromExistingData(int businessId)
        {
            // Rezervasyonlardan
            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == businessId && !string.IsNullOrEmpty(r.UserId))
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.RelatedReservationId == reservation.Id);

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        BusinessId = businessId,
                        CustomerId = reservation.UserId!,
                        InteractionType = "Reservation",
                        Subject = $"{reservation.NumberOfPeople} kişilik rezervasyon",
                        Notes = reservation.Notes,
                        InteractionDate = reservation.CreatedDate,
                        Status = reservation.Status == "Onaylandı" ? "Completed" : "Pending",
                        RelatedReservationId = reservation.Id,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            // Yorumlardan
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && !string.IsNullOrEmpty(r.UserId) && r.IsActive && r.IsApproved)
                .ToListAsync();

            foreach (var review in reviews)
            {
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.RelatedReviewId == review.Id);

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        BusinessId = businessId,
                        CustomerId = review.UserId!,
                        InteractionType = "Review",
                        Subject = $"{review.Rating} yıldız yorum",
                        Notes = review.Comment,
                        InteractionDate = review.CreatedDate,
                        Status = "Completed",
                        RelatedReviewId = review.Id,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            // Mesajlardan
            var messages = await _context.Messages
                .Where(m => m.RelatedBusinessId == businessId)
                .ToListAsync();

            foreach (var message in messages)
            {
                var customerId = message.SenderId; // İşletme sahibine mesaj gönderen müşteri
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.RelatedMessageId == message.Id);

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        BusinessId = businessId,
                        CustomerId = customerId,
                        InteractionType = "Message",
                        Subject = message.Subject,
                        Notes = message.Content,
                        InteractionDate = message.CreatedDate,
                        Status = message.IsRead ? "Completed" : "Pending",
                        RelatedMessageId = message.Id,
                        CreatedDate = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
