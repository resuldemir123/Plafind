using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Plafind.Controllers
{
    [AllowAnonymous]
    public class CompareController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CompareSessionKey = "CompareBusinesses";

        public CompareController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Karşılaştırma listesini al
        private List<int> GetCompareList()
        {
            var compareJson = HttpContext.Session.GetString(CompareSessionKey);
            if (string.IsNullOrEmpty(compareJson))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(compareJson) ?? new List<int>();
            }
            catch
            {
                return new List<int>();
            }
        }

        // Karşılaştırma listesini kaydet
        private void SaveCompareList(List<int> businessIds)
        {
            var compareJson = JsonSerializer.Serialize(businessIds);
            HttpContext.Session.SetString(CompareSessionKey, compareJson);
        }

        // GET: Compare
        public async Task<IActionResult> Index()
        {
            var compareIds = GetCompareList();
            
            if (compareIds.Count == 0)
            {
                ViewBag.Message = "Karşılaştırma listeniz boş. İşletme detay sayfalarından işletmeleri karşılaştırmaya ekleyebilirsiniz.";
                return View(new List<Business>());
            }

            var businesses = await _context.Businesses
                .Where(b => compareIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Include(b => b.Reviews.Where(r => r.IsActive && r.IsApproved))
                .Include(b => b.Images.Where(img => img.IsActive))
                .Include(b => b.Campaigns.Where(c => c.IsActive && c.IsApproved && c.EndDate >= DateTime.Now))
                .Include(b => b.Events.Where(e => e.IsActive && e.IsApproved && e.StartDate >= DateTime.Now))
                .ToListAsync();

            // Sıralamayı koru
            var orderedBusinesses = compareIds
                .Select(id => businesses.FirstOrDefault(b => b.Id == id))
                .Where(b => b != null)
                .ToList();

            return View(orderedBusinesses);
        }

        // POST: Compare/Add
        [HttpPost]
        public IActionResult Add(int businessId)
        {
            var compareIds = GetCompareList();

            if (compareIds.Contains(businessId))
            {
                return Json(new { success = false, message = "Bu işletme zaten karşılaştırma listesinde." });
            }

            if (compareIds.Count >= 4)
            {
                return Json(new { success = false, message = "En fazla 4 işletme karşılaştırabilirsiniz." });
            }

            compareIds.Add(businessId);
            SaveCompareList(compareIds);

            return Json(new { success = true, message = "İşletme karşılaştırmaya eklendi.", count = compareIds.Count });
        }

        // POST: Compare/Remove
        [HttpPost]
        public IActionResult Remove(int businessId)
        {
            var compareIds = GetCompareList();
            compareIds.Remove(businessId);
            SaveCompareList(compareIds);

            return Json(new { success = true, message = "İşletme karşılaştırmadan kaldırıldı.", count = compareIds.Count });
        }

        // POST: Compare/Clear
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CompareSessionKey);
            return Json(new { success = true, message = "Karşılaştırma listesi temizlendi." });
        }

        // GET: Compare/Count
        [HttpGet]
        public IActionResult GetCount()
        {
            var compareIds = GetCompareList();
            return Json(new { count = compareIds.Count });
        }

        // GET: Compare/Check
        [HttpGet]
        public IActionResult Check(int businessId)
        {
            var compareIds = GetCompareList();
            return Json(new { isInCompare = compareIds.Contains(businessId) });
        }

        // GET: Compare/GetList
        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var compareIds = GetCompareList();
            
            if (compareIds.Count == 0)
            {
                return Json(new { businesses = new List<object>() });
            }

            var businesses = await _context.Businesses
                .Where(b => compareIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Select(b => new
                {
                    id = b.Id,
                    name = b.Name ?? "İsimsiz",
                    category = b.Category != null ? b.Category.Name : null,
                    imageUrl = b.ImageUrl,
                    rating = b.AverageRating
                })
                .ToListAsync();

            // Sıralamayı koru
            var orderedBusinesses = compareIds
                .Select(id => businesses.FirstOrDefault(b => b.id == id))
                .Where(b => b != null)
                .ToList();

            return Json(new { businesses = orderedBusinesses });
        }
    }
}
