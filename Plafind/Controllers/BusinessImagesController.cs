using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace Plafind.Controllers
{
    [Authorize(Roles = "BusinessOwner,Admin")]
    public class BusinessImagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public BusinessImagesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> GetImages(int businessId)
        {
            var images = await _context.BusinessImages
                .Where(img => img.BusinessId == businessId && img.IsActive)
                .OrderBy(img => img.DisplayOrder)
                .ThenByDescending(img => img.IsPrimary)
                .ToListAsync();

            return Json(images);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(int businessId, IFormFile[] files)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId);

            if (business == null)
                return Json(new { success = false, message = "İşletme bulunamadı" });

            // İşletme sahibi kontrolü
            if (!User.IsInRole("Admin") && business.OwnerId != userId)
                return Json(new { success = false, message = "Bu işletmeye resim yükleme yetkiniz yok" });

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "businesses", businessId.ToString());
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uploadedImages = new List<BusinessImage>();
            var maxOrder = await _context.BusinessImages
                .Where(img => img.BusinessId == businessId)
                .MaxAsync(img => (int?)img.DisplayOrder) ?? 0;

            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                    continue;

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var businessImage = new BusinessImage
                {
                    BusinessId = businessId,
                    ImageUrl = $"/uploads/businesses/{businessId}/{fileName}",
                    DisplayOrder = ++maxOrder,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = userId
                };

                _context.BusinessImages.Add(businessImage);
                uploadedImages.Add(businessImage);
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"{uploadedImages.Count} resim başarıyla yüklendi", images = uploadedImages });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimary(int imageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var image = await _context.BusinessImages
                .Include(img => img.Business)
                .FirstOrDefaultAsync(img => img.Id == imageId);

            if (image == null)
                return Json(new { success = false, message = "Resim bulunamadı" });

            // İşletme sahibi kontrolü
            if (!User.IsInRole("Admin") && image.Business?.OwnerId != userId)
                return Json(new { success = false, message = "Bu işlemi yapma yetkiniz yok" });

            // Diğer resimlerden primary'i kaldır
            var otherImages = await _context.BusinessImages
                .Where(img => img.BusinessId == image.BusinessId && img.Id != imageId)
                .ToListAsync();

            foreach (var otherImage in otherImages)
            {
                otherImage.IsPrimary = false;
            }

            image.IsPrimary = true;
            _context.BusinessImages.UpdateRange(otherImages);
            _context.BusinessImages.Update(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Ana resim olarak ayarlandı" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int imageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var image = await _context.BusinessImages
                .Include(img => img.Business)
                .FirstOrDefaultAsync(img => img.Id == imageId);

            if (image == null)
                return Json(new { success = false, message = "Resim bulunamadı" });

            // İşletme sahibi kontrolü
            if (!User.IsInRole("Admin") && image.Business?.OwnerId != userId)
                return Json(new { success = false, message = "Bu resmi silme yetkiniz yok" });

            // Fiziksel dosyayı sil
            if (!string.IsNullOrEmpty(image.ImageUrl) && image.ImageUrl.StartsWith("/uploads/"))
            {
                var filePath = Path.Combine(_environment.WebRootPath, image.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.BusinessImages.Remove(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Resim başarıyla silindi" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int[] imageIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            for (int i = 0; i < imageIds.Length; i++)
            {
                var image = await _context.BusinessImages
                    .Include(img => img.Business)
                    .FirstOrDefaultAsync(img => img.Id == imageIds[i]);

                if (image != null)
                {
                    // İşletme sahibi kontrolü
                    if (!User.IsInRole("Admin") && image.Business?.OwnerId != userId)
                        continue;

                    image.DisplayOrder = i + 1;
                    _context.BusinessImages.Update(image);
                }
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Sıralama güncellendi" });
        }
    }
}
