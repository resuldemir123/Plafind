using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using System.Linq;

namespace Plafind.Controllers
{
    public class CampaignsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CampaignsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? businessId = null, string? campaignType = null)
        {
            var query = _context.Campaigns
                .Where(c => c.IsActive && c.IsApproved && 
                           c.StartDate <= DateTime.Now && 
                           c.EndDate >= DateTime.Now)
                .Include(c => c.Business)
                .ThenInclude(b => b.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(businessId) && int.TryParse(businessId, out int bid))
            {
                query = query.Where(c => c.BusinessId == bid);
            }

            if (!string.IsNullOrEmpty(campaignType))
            {
                query = query.Where(c => c.CampaignType == campaignType);
            }

            var campaigns = await query
                .OrderByDescending(c => c.IsFeatured)
                .ThenByDescending(c => c.CreatedDate)
                .ToListAsync();

            ViewBag.CampaignTypes = await _context.Campaigns
                .Where(c => c.IsActive && c.IsApproved)
                .Select(c => c.CampaignType)
                .Distinct()
                .ToListAsync();

            return View(campaigns);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Business)
                .ThenInclude(b => b.Category)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
                return NotFound();

            return View(campaign);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ApplyCoupon(string couponCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            if (string.IsNullOrWhiteSpace(couponCode))
                return Json(new { success = false, message = "Kupon kodu boş olamaz" });

            var campaign = await _context.Campaigns
                .FirstOrDefaultAsync(c => c.CouponCode == couponCode.ToUpper() &&
                                         c.IsActive &&
                                         c.IsApproved &&
                                         c.StartDate <= DateTime.Now &&
                                         c.EndDate >= DateTime.Now);

            if (campaign == null)
                return Json(new { success = false, message = "Geçersiz veya süresi dolmuş kupon kodu" });

            // Kullanım limiti kontrolü
            if (campaign.MaxUses.HasValue && campaign.CurrentUses >= campaign.MaxUses.Value)
                return Json(new { success = false, message = "Bu kuponun kullanım limiti dolmuş" });

            // Kullanıcı başına kullanım limiti kontrolü
            var userUsageCount = await _context.CampaignUsages
                .CountAsync(u => u.CampaignId == campaign.Id && u.UserId == userId);

            if (campaign.MaxUsesPerUser.HasValue && userUsageCount >= campaign.MaxUsesPerUser.Value)
                return Json(new { success = false, message = "Bu kuponu daha fazla kullanamazsınız" });

            // Kullanım kaydı oluştur
            var usage = new CampaignUsage
            {
                CampaignId = campaign.Id,
                UserId = userId,
                UsedDate = DateTime.Now,
                DiscountApplied = campaign.DiscountAmount ?? (campaign.DiscountPercentage.HasValue ? campaign.DiscountPercentage.Value : 0)
            };

            _context.CampaignUsages.Add(usage);
            campaign.CurrentUses++;
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Kupon başarıyla uygulandı",
                discount = campaign.DiscountAmount,
                discountPercentage = campaign.DiscountPercentage
            });
        }

        [Authorize(Roles = "BusinessOwner,Admin")]
        public IActionResult Create(int businessId)
        {
            var business = _context.Businesses.Find(businessId);
            if (business == null)
                return NotFound();

            ViewBag.Business = business;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner,Admin")]
        public async Task<IActionResult> Create(Campaign campaign, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var business = await _context.Businesses.FindAsync(campaign.BusinessId);
            if (business == null)
                return NotFound();

            // İşletme sahibi kontrolü
            if (!User.IsInRole("Admin") && business.OwnerId != userId)
                return Forbid();

            if (ModelState.IsValid)
            {
                campaign.CreatedBy = userId;
                campaign.CreatedDate = DateTime.Now;
                campaign.IsActive = true;
                campaign.IsApproved = User.IsInRole("Admin");

                // Kupon kodu oluştur
                if (string.IsNullOrEmpty(campaign.CouponCode))
                {
                    campaign.CouponCode = GenerateCouponCode();
                }
                else
                {
                    campaign.CouponCode = campaign.CouponCode.ToUpper();
                }

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

                _context.Campaigns.Add(campaign);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Kampanya başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Details), new { id = campaign.Id });
            }

            ViewBag.Business = business;
            return View(campaign);
        }

        private string GenerateCouponCode()
        {
            var random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
