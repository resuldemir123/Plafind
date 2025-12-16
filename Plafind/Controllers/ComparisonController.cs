using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Plafind.Models;
using Plafind.Services;

namespace Plafind.Controllers
{
    /// <summary>
    /// İşletme karşılaştırma controller'ı
    /// </summary>
    [AllowAnonymous]
    public class ComparisonController : Controller
    {
        private readonly IComparisonService _comparisonService;
        private readonly ILogger<ComparisonController> _logger;

        public ComparisonController(
            IComparisonService comparisonService,
            ILogger<ComparisonController> logger)
        {
            _comparisonService = comparisonService ?? throw new ArgumentNullException(nameof(comparisonService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Karşılaştırma sayfası
        /// </summary>
        public async Task<IActionResult> Index(string? ids)
        {
            List<int> businessIds = new List<int>();

            // Query string'den ID'leri al
            if (!string.IsNullOrWhiteSpace(ids))
            {
                var idStrings = ids.Split(',', StringSplitOptions.RemoveEmptyEntries);
                foreach (var idString in idStrings)
                {
                    if (int.TryParse(idString.Trim(), out int id) && id > 0)
                    {
                        businessIds.Add(id);
                    }
                }
            }

            // POST body'den de kontrol et (AJAX için)
            if (Request.Method == "POST" && Request.HasFormContentType)
            {
                var formIds = Request.Form["ids"].ToString();
                if (!string.IsNullOrWhiteSpace(formIds))
                {
                    var idStrings = formIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var idString in idStrings)
                    {
                        if (int.TryParse(idString.Trim(), out int id) && id > 0 && !businessIds.Contains(id))
                        {
                            businessIds.Add(id);
                        }
                    }
                }
            }

            // Validasyon
            if (!businessIds.Any())
            {
                _logger.LogWarning("Comparison Index: Hiç işletme ID'si gönderilmedi");
                ViewBag.ErrorMessage = "Karşılaştırma için en az bir işletme seçmelisiniz.";
                return View(new ComparisonViewModel());
            }

            // Maksimum 4 işletme limiti
            if (businessIds.Count > 4)
            {
                _logger.LogWarning("Comparison Index: {Count} işletme gönderildi, maksimum 4'e sınırlandı", businessIds.Count);
                businessIds = businessIds.Take(4).ToList();
                ViewBag.WarningMessage = "Maksimum 4 işletme karşılaştırılabilir. İlk 4 işletme seçildi.";
            }

            try
            {
                // İşletmeleri çek
                var businesses = await _comparisonService.GetBusinessesForComparisonAsync(businessIds);

                if (!businesses.Any())
                {
                    ViewBag.ErrorMessage = "Seçilen işletmeler bulunamadı veya aktif değil.";
                    return View(new ComparisonViewModel());
                }

                // Karşılaştırma matrisi oluştur
                var viewModel = _comparisonService.CreateComparisonMatrix(businesses);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Comparison Index: Hata oluştu");
                ViewBag.ErrorMessage = "Karşılaştırma yapılırken bir hata oluştu. Lütfen tekrar deneyin.";
                return View(new ComparisonViewModel());
            }
        }

        /// <summary>
        /// AJAX endpoint: İşletmeyi karşılaştırmaya ekle
        /// </summary>
        [HttpPost]
        public IActionResult AddToComparison(int businessId)
        {
            if (businessId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz işletme ID'si" });
            }

            return Json(new { success = true, businessId = businessId });
        }

        /// <summary>
        /// AJAX endpoint: İşletmeyi karşılaştırmadan çıkar
        /// </summary>
        [HttpPost]
        public IActionResult RemoveFromComparison(int businessId)
        {
            if (businessId <= 0)
            {
                return Json(new { success = false, message = "Geçersiz işletme ID'si" });
            }

            return Json(new { success = true, businessId = businessId });
        }

        /// <summary>
        /// AJAX endpoint: Karşılaştırma verilerini JSON olarak döndür
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetComparisonData(string ids)
        {
            if (string.IsNullOrWhiteSpace(ids))
            {
                return Json(new { success = false, message = "İşletme ID'leri gönderilmedi" });
            }

            var businessIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out int result) ? result : 0)
                .Where(id => id > 0)
                .Distinct()
                .Take(4)
                .ToList();

            if (!businessIds.Any())
            {
                return Json(new { success = false, message = "Geçerli işletme ID'si bulunamadı" });
            }

            try
            {
                var businesses = await _comparisonService.GetBusinessesForComparisonAsync(businessIds);
                var viewModel = _comparisonService.CreateComparisonMatrix(businesses);

                return Json(new
                {
                    success = true,
                    businesses = businesses.Select(b => new
                    {
                        id = b.Id,
                        name = b.Name,
                        imageUrl = b.ImageUrl,
                        category = b.Category?.Name
                    }),
                    criticalFeatures = viewModel.CriticalFeatures,
                    featureRows = viewModel.FeatureRows
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetComparisonData: Hata oluştu");
                return Json(new { success = false, message = "Veri çekilirken hata oluştu" });
            }
        }
    }
}

