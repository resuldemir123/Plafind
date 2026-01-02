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

            // CustomerInteraction artık Customer modeline bağlı
            var customerModel = await _context.Customers
                .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.UserId == customerId);
            
            var interactions = customerModel != null 
                ? await _context.CustomerInteractions
                    .Where(ci => ci.CustomerId == customerModel.Id)
                    .OrderByDescending(ci => ci.InteractionDate)
                    .ToListAsync()
                : new List<CustomerInteraction>();

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

            // CustomerInteraction artık Customer modeline bağlı
            IQueryable<CustomerInteraction> interactionsQuery;
            
            if (!string.IsNullOrEmpty(customerId))
            {
                var customerModel = await _context.Customers
                    .FirstOrDefaultAsync(c => c.BusinessId == business.Id && c.UserId == customerId);
                
                if (customerModel != null)
                {
                    interactionsQuery = _context.CustomerInteractions
                        .Where(ci => ci.CustomerId == customerModel.Id)
                        .Include(ci => ci.Customer);
                }
                else
                {
                    interactionsQuery = _context.CustomerInteractions
                        .Where(ci => false) // Boş sonuç
                        .Include(ci => ci.Customer);
                }
            }
            else
            {
                var customerIds = await _context.Customers
                    .Where(c => c.BusinessId == business.Id)
                    .Select(c => c.Id)
                    .ToListAsync();
                
                interactionsQuery = _context.CustomerInteractions
                    .Where(ci => customerIds.Contains(ci.CustomerId))
                    .Include(ci => ci.Customer);
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
        public async Task<IActionResult> CreateInteraction(int customerId, string interactionType, string notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null || customer.Business == null)
            {
                return NotFound();
            }

            if (!User.IsInRole("Admin") && customer.Business.OwnerId != userId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                var interaction = new CustomerInteraction
                {
                    CustomerId = customerId,
                    InteractionType = interactionType,
                    Notes = notes,
                    InteractionDate = DateTime.Now,
                    CreatedBy = userId
                };
                
                _context.Add(interaction);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Interactions), new { businessId = customer.BusinessId, customerId = customer.UserId });
            }

            return RedirectToAction(nameof(Interactions), new { businessId = customer.BusinessId });
        }

        private async Task CreateInteractionsFromExistingData(int businessId)
        {
            // Rezervasyonlardan - Customer modeli üzerinden
            var reservations = await _context.Reservations
                .Where(r => r.BusinessId == businessId && r.CustomerId != null)
                .Include(r => r.Customer)
                .ToListAsync();

            foreach (var reservation in reservations)
            {
                if (reservation.Customer == null) continue;
                
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.CustomerId == reservation.Customer.Id && 
                                               ci.InteractionType == "Reservation" &&
                                               ci.Notes != null && 
                                               ci.Notes.Contains($"Rezervasyon #{reservation.Id}"));

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        CustomerId = reservation.Customer.Id,
                        InteractionType = "Reservation",
                        Notes = $"{reservation.NumberOfPeople} kişilik rezervasyon - Rezervasyon #{reservation.Id}. {reservation.Notes}",
                        InteractionDate = reservation.CreatedDate,
                        CreatedBy = reservation.Customer.UserId
                    });
                }
            }

            // Yorumlardan - Customer modeli üzerinden
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && !string.IsNullOrEmpty(r.UserId) && r.IsActive && r.IsApproved)
                .ToListAsync();

            foreach (var review in reviews)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.UserId == review.UserId);
                
                if (customer == null) continue;
                
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.CustomerId == customer.Id && 
                                               ci.InteractionType == "Review" &&
                                               ci.Notes != null && 
                                               ci.Notes.Contains($"Yorum #{review.Id}"));

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        CustomerId = customer.Id,
                        InteractionType = "Review",
                        Notes = $"{review.Rating} yıldız yorum - Yorum #{review.Id}. {review.Comment}",
                        InteractionDate = review.CreatedDate,
                        CreatedBy = review.UserId
                    });
                }
            }

            // Mesajlardan - Customer modeli üzerinden
            var messages = await _context.Messages
                .Where(m => m.RelatedBusinessId == businessId && !string.IsNullOrEmpty(m.SenderId))
                .ToListAsync();

            foreach (var message in messages)
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.UserId == message.SenderId);
                
                if (customer == null) continue;
                
                var existing = await _context.CustomerInteractions
                    .FirstOrDefaultAsync(ci => ci.CustomerId == customer.Id && 
                                               ci.InteractionType == "Message" &&
                                               ci.Notes != null && 
                                               ci.Notes.Contains($"Mesaj #{message.Id}"));

                if (existing == null)
                {
                    _context.CustomerInteractions.Add(new CustomerInteraction
                    {
                        CustomerId = customer.Id,
                        InteractionType = "Message",
                        Notes = $"Mesaj: {message.Subject} - Mesaj #{message.Id}. {message.Content}",
                        InteractionDate = message.CreatedDate,
                        CreatedBy = message.SenderId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
