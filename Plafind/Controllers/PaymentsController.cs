using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using Plafind.ViewModels;
using System.Security.Claims;

namespace Plafind.Controllers
{
    [Authorize]
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var payments = await _context.Payments
                .Where(p => p.UserId == userId)
                .Include(p => p.Business)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            return View(payments);
        }

        public async Task<IActionResult> Plans()
        {
            var plans = GetSubscriptionPlans();
            return View(plans);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string planType, int? businessId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var plans = GetSubscriptionPlans();
            var selectedPlan = plans.FirstOrDefault(p => p.Id == planType);
            
            if (selectedPlan == null)
            {
                TempData["ErrorMessage"] = "Geçersiz plan seçildi.";
                return RedirectToAction(nameof(Plans));
            }

            var model = new PaymentViewModel
            {
                PlanType = planType,
                PaymentType = "Subscription",
                BusinessId = businessId,
                Amount = selectedPlan.Price
            };

            ViewBag.Plan = selectedPlan;
            if (businessId.HasValue)
            {
                ViewBag.Business = await _context.Businesses.FindAsync(businessId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                var plans = GetSubscriptionPlans();
                var selectedPlan = plans.FirstOrDefault(p => p.Id == model.PlanType);
                ViewBag.Plan = selectedPlan;
                return View(model);
            }

            try
            {
                // Ödeme kaydı oluştur
                var payment = new Payment
                {
                    UserId = userId,
                    BusinessId = model.BusinessId,
                    PaymentType = model.PaymentType,
                    PlanType = model.PlanType,
                    Amount = model.Amount,
                    Currency = "TRY",
                    Status = "Pending",
                    PaymentProvider = "Manual", // Gerçek entegrasyon için iyzico, PayTR vb. kullanılabilir
                    PaymentMethod = "CreditCard",
                    CreatedDate = DateTime.Now
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                // TODO: Gerçek ödeme gateway entegrasyonu burada yapılacak
                // Şimdilik manuel olarak başarılı kabul ediyoruz
                payment.Status = "Completed";
                payment.PaymentDate = DateTime.Now;
                payment.TransactionId = $"TXN-{payment.Id}-{DateTime.Now:yyyyMMddHHmmss}";
                
                // Abonelik oluştur
                var plan = GetSubscriptionPlans().FirstOrDefault(p => p.Id == model.PlanType);
                if (plan != null)
                {
                    var subscription = new Subscription
                    {
                        UserId = userId,
                        BusinessId = model.BusinessId,
                        PlanType = model.PlanType,
                        StartDate = DateTime.Now,
                        EndDate = DateTime.Now.AddDays(plan.DurationDays),
                        Status = "Active",
                        AutoRenew = false,
                        CreatedDate = DateTime.Now
                    };

                    _context.Subscriptions.Add(subscription);
                }

                _context.Payments.Update(payment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Ödemeniz başarıyla tamamlandı!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Ödeme işlemi sırasında bir hata oluştu.");
                return View(model);
            }
        }

        public async Task<IActionResult> Subscriptions()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .Include(s => s.Business)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            return View(subscriptions);
        }

        [HttpPost]
        public async Task<IActionResult> CancelSubscription(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

            if (subscription == null)
                return Json(new { success = false, message = "Abonelik bulunamadı" });

            subscription.Status = "Cancelled";
            subscription.AutoRenew = false;
            subscription.CancelledDate = DateTime.Now;
            subscription.UpdatedDate = DateTime.Now;

            _context.Subscriptions.Update(subscription);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Abonelik iptal edildi" });
        }

        private List<SubscriptionPlan> GetSubscriptionPlans()
        {
            return new List<SubscriptionPlan>
            {
                new SubscriptionPlan
                {
                    Id = "basic",
                    Name = "Temel Plan",
                    Description = "Temel özellikler",
                    Price = 99.00m,
                    DurationDays = 30,
                    Features = new List<string>
                    {
                        "1 İşletme",
                        "Temel profil",
                        "Rezervasyon yönetimi",
                        "Email desteği"
                    }
                },
                new SubscriptionPlan
                {
                    Id = "premium",
                    Name = "Premium Plan",
                    Description = "Gelişmiş özellikler",
                    Price = 199.00m,
                    DurationDays = 30,
                    Features = new List<string>
                    {
                        "3 İşletme",
                        "Gelişmiş profil",
                        "Öncelikli destek",
                        "Analytics",
                        "Kampanya yönetimi"
                    }
                },
                new SubscriptionPlan
                {
                    Id = "enterprise",
                    Name = "Kurumsal Plan",
                    Description = "Tüm özellikler",
                    Price = 499.00m,
                    DurationDays = 30,
                    Features = new List<string>
                    {
                        "Sınırsız işletme",
                        "Özel destek",
                        "API erişimi",
                        "Gelişmiş analytics",
                        "Özel entegrasyonlar"
                    }
                }
            };
        }
    }
}
