using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Plafind.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Plafind.Controllers
{
    public class NewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IGoogleNewsService _googleNewsService;

        public NewsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IConfiguration configuration,
            IGoogleNewsService googleNewsService)
        {
            _context = context;
            _userManager = userManager;
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _googleNewsService = googleNewsService ?? throw new ArgumentNullException(nameof(googleNewsService));
        }

        // GET: News
        public async Task<IActionResult> Index()
        {
            var news = await _context.News
                .Include(n => n.Author)
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();
            return View(news);
        }

        // GET: News/SyncTourismNews
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncTourismNews()
        {
            try
            {
                await _googleNewsService.SyncTourismNewsToDatabaseAsync(maxItems: 20);
                TempData["SuccessMessage"] = "Turizm haberleri başarıyla senkronize edildi.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Haberler senkronize edilirken bir hata oluştu: {ex.Message}";
            }
            
            return RedirectToAction(nameof(Index));
        }

        // GET: News/PreviewTourismNews
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PreviewTourismNews()
        {
            var news = await _googleNewsService.GetAlanyaTourismNewsAsync(maxItems: 20);
            return View(news);
        }

        // GET: News/SyncStatus
        [Authorize(Roles = "Admin")]
        public IActionResult SyncStatus()
        {
            var syncStatus = GoogleNewsService.GetSyncStatus();
            var (isRunning, startTime, totalRuns, lastRunTime) = TourismNewsBackgroundService.GetServiceStatus();
            
            syncStatus.IsBackgroundServiceRunning = isRunning;
            syncStatus.ServiceStartTime = startTime != DateTime.MinValue ? startTime : null;
            syncStatus.ServiceTotalRuns = totalRuns;
            syncStatus.ServiceLastRunTime = lastRunTime != DateTime.MinValue ? lastRunTime : null;
            
            if (syncStatus.LastSyncTime != DateTime.MinValue)
            {
                syncStatus.TimeSinceLastSync = DateTime.Now - syncStatus.LastSyncTime;
            }
            
            return View(syncStatus);
        }

        // GET: News/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .Include(n => n.Author)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (news == null)
            {
                return NotFound();
            }

            var siteUrl = _configuration["SiteSettings:SiteUrl"] ?? "https://plafind.com";
            var siteName = _configuration["SiteSettings:SiteName"] ?? "Plafind";
            
            // Open Graph bilgilerini set et
            ViewData["SiteUrl"] = siteUrl;
            ViewData["SiteName"] = siteName;
            ViewData["OgTitle"] = $"{news.Title} | {siteName}";
            ViewData["OgDescription"] = !string.IsNullOrWhiteSpace(news.Content) 
                ? (news.Content.Length > 200 ? news.Content.Substring(0, 200).Replace("<p>", "").Replace("</p>", "").Replace("<br>", " ").Replace("<br/>", " ").Trim() + "..." : news.Content.Replace("<p>", "").Replace("</p>", "").Replace("<br>", " ").Replace("<br/>", " ").Trim())
                : $"{news.Title} - {siteName}";
            ViewData["OgImage"] = !string.IsNullOrWhiteSpace(news.ImageUrl) 
                ? (news.ImageUrl.StartsWith("http") ? news.ImageUrl : $"{siteUrl}{news.ImageUrl}")
                : $"{siteUrl}/images/Logo.png";
            ViewData["OgUrl"] = $"{siteUrl}/News/Details/{news.Id}";
            ViewData["OgType"] = "article";

            // Görüntülenme sayısını artır (isteğe bağlı)
            if (news.ViewCount == null)
            {
                news.ViewCount = 0;
            }
            news.ViewCount++;
            await _context.SaveChangesAsync();

            return View(news);
        }

        // GET: News/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: News/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(News news)
        {
            if (ModelState.IsValid)
            {
                news.AuthorId = (await _userManager.GetUserAsync(User))?.Id;
                news.PublishDate = DateTime.Now;
                news.ViewCount = 0;
                _context.News.Add(news);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }

        // GET: News/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News.FindAsync(id);
            if (news == null)
            {
                return NotFound();
            }
            return View(news);
        }

        // POST: News/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, News news)
        {
            if (id != news.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(news);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NewsExists(news.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(news);
        }

        // GET: News/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var news = await _context.News
                .Include(n => n.Author)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (news == null)
            {
                return NotFound();
            }

            return View(news);
        }

        // POST: News/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news != null)
            {
                _context.News.Remove(news);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool NewsExists(int id)
        {
            return _context.News.Any(e => e.Id == id);
        }
    }
}