using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace Plafind.Controllers
{
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public EventsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(string? businessId = null, string? eventType = null, DateTime? startDate = null)
        {
            var query = _context.Events
                .Where(e => e.IsActive && e.IsApproved)
                .Include(e => e.Business)
                .ThenInclude(b => b.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(businessId) && int.TryParse(businessId, out int bid))
            {
                query = query.Where(e => e.BusinessId == bid);
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.StartDate >= startDate.Value);
            }

            var events = await query
                .OrderBy(e => e.StartDate)
                .ToListAsync();

            ViewBag.EventTypes = await _context.Events
                .Where(e => e.IsActive && e.IsApproved)
                .Select(e => e.EventType)
                .Distinct()
                .ToListAsync();

            return View(events);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var eventItem = await _context.Events
                .Include(e => e.Business)
                .ThenInclude(b => b.Category)
                .Include(e => e.Attendees)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventItem == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.IsRegistered = await _context.EventAttendees
                    .AnyAsync(a => a.EventId == id && a.UserId == userId);
            }

            return View(eventItem);
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
        public async Task<IActionResult> Create(Event eventItem, IFormFile? imageFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var business = await _context.Businesses.FindAsync(eventItem.BusinessId);
            if (business == null)
                return NotFound();

            // İşletme sahibi kontrolü
            if (!User.IsInRole("Admin") && business.OwnerId != userId)
                return Forbid();

            if (ModelState.IsValid)
            {
                eventItem.CreatedBy = userId;
                eventItem.CreatedDate = DateTime.Now;
                eventItem.IsActive = true;
                eventItem.IsApproved = User.IsInRole("Admin");

                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "events");
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

                    eventItem.ImageUrl = $"/uploads/events/{fileName}";
                }

                _context.Events.Add(eventItem);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Etkinlik başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Details), new { id = eventItem.Id });
            }

            ViewBag.Business = business;
            return View(eventItem);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Register(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem == null)
                return Json(new { success = false, message = "Etkinlik bulunamadı" });

            // Zaten kayıtlı mı kontrol et
            var existing = await _context.EventAttendees
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

            if (existing != null)
                return Json(new { success = false, message = "Bu etkinliğe zaten kayıtlısınız" });

            // Kontenjan kontrolü
            if (eventItem.MaxAttendees.HasValue && eventItem.CurrentAttendees >= eventItem.MaxAttendees.Value)
                return Json(new { success = false, message = "Etkinlik kontenjanı dolmuş" });

            var attendee = new EventAttendee
            {
                EventId = eventId,
                UserId = userId,
                RegisteredDate = DateTime.Now,
                IsConfirmed = false
            };

            _context.EventAttendees.Add(attendee);
            eventItem.CurrentAttendees++;
            _context.Events.Update(eventItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Etkinliğe başarıyla kayıt oldunuz" });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Unregister(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var attendee = await _context.EventAttendees
                .FirstOrDefaultAsync(a => a.EventId == eventId && a.UserId == userId);

            if (attendee == null)
                return Json(new { success = false, message = "Bu etkinliğe kayıtlı değilsiniz" });

            var eventItem = await _context.Events.FindAsync(eventId);
            if (eventItem != null)
            {
                eventItem.CurrentAttendees = Math.Max(0, eventItem.CurrentAttendees - 1);
                _context.Events.Update(eventItem);
            }

            _context.EventAttendees.Remove(attendee);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Kayıt iptal edildi" });
        }
    }
}
