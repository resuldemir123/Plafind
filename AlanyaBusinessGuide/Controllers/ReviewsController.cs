using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AlanyaBusinessGuide.Data;
using AlanyaBusinessGuide.Models;
using Microsoft.EntityFrameworkCore;

namespace AlanyaBusinessGuide.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int businessId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId)
                .Include(r => r.User)
                .ToListAsync();
            ViewBag.BusinessId = businessId;
            return View(reviews);
        }

        public IActionResult Create(int businessId)
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }
            ViewBag.BusinessId = businessId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Review review)
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    review.UserId = user.Id; // UserId doğrudan Id'den alınır
                }
                review.CreatedDate = DateTime.Now;
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { businessId = review.BusinessId });
            }
            ViewBag.BusinessId = review.BusinessId;
            return View(review);
        }
    }
}