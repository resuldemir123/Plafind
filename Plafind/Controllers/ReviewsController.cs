using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Plafind.Data;
using Plafind.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Plafind.Controllers
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

        [AllowAnonymous]
        public async Task<IActionResult> Index(int businessId, string? sortBy = "newest", int? minRating = null, int? maxRating = null)
        {
            var query = _context.Reviews
                .Where(r => r.BusinessId == businessId && r.IsActive && r.IsApproved)
                .Include(r => r.User)
                .Include(r => r.Likes)
                .AsQueryable();

            // Rating filtreleme
            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            // Sıralama
            switch (sortBy?.ToLower())
            {
                case "highest":
                    query = query.OrderByDescending(r => r.Rating);
                    break;
                case "lowest":
                    query = query.OrderBy(r => r.Rating);
                    break;
                case "mostliked":
                    query = query.OrderByDescending(r => r.Likes.Count(l => l.IsLike));
                    break;
                case "oldest":
                    query = query.OrderBy(r => r.CreatedDate);
                    break;
                default: // "newest"
                    query = query.OrderByDescending(r => r.CreatedDate);
                    break;
            }

            var reviews = await query.ToListAsync();

            // Like/Dislike sayılarını hesapla
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            foreach (var review in reviews)
            {
                review.LikeCount = review.Likes.Count(l => l.IsLike);
                review.DislikeCount = review.Likes.Count(l => !l.IsLike);
                
                if (!string.IsNullOrEmpty(userId))
                {
                    ViewBag.UserLikes = await _context.ReviewLikes
                        .Where(l => l.UserId == userId && reviews.Select(r => r.Id).Contains(l.ReviewId))
                        .ToDictionaryAsync(l => l.ReviewId, l => l.IsLike);
                }
            }

            ViewBag.BusinessId = businessId;
            ViewBag.SortBy = sortBy;
            ViewBag.MinRating = minRating;
            ViewBag.MaxRating = maxRating;
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
                review.IsApproved = true; // Yorumlar otomatik onaylanır
                review.IsActive = true;
                _context.Reviews.Add(review);
                
                // İşletme puanını güncelle
                var business = await _context.Businesses.FindAsync(review.BusinessId);
                if (business != null)
                {
                    var allReviews = await _context.Reviews
                        .Where(r => r.BusinessId == review.BusinessId && r.IsApproved && r.IsActive)
                        .ToListAsync();

                    business.AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
                    business.TotalReviews = allReviews.Count;
                    _context.Businesses.Update(business);
                }
                
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Businesses", new { id = review.BusinessId });
            }
            ViewBag.BusinessId = review.BusinessId;
            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner,Admin")]
        public async Task<IActionResult> Reply(int reviewId, string replyText)
        {
            if (string.IsNullOrWhiteSpace(replyText))
            {
                return Json(new { success = false, message = "Yanıt metni boş olamaz." });
            }

            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
            {
                return Json(new { success = false, message = "Yorum bulunamadı." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // İşletme sahibi kontrolü
            if (User.IsInRole("BusinessOwner"))
            {
                if (review.Business?.OwnerId != user.Id)
                {
                    return Json(new { success = false, message = "Bu yoruma yanıt verme yetkiniz yok." });
                }
            }

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = user.Id,
                ReplyText = replyText,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsApproved = true
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yanıt başarıyla eklendi.", replyId = reply.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner,Admin")]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var reply = await _context.ReviewReplies
                .Include(r => r.Review)
                .ThenInclude(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null)
            {
                return Json(new { success = false, message = "Yanıt bulunamadı." });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // İşletme sahibi veya admin kontrolü
            if (User.IsInRole("BusinessOwner"))
            {
                if (reply.Review?.Business?.OwnerId != user.Id)
                {
                    return Json(new { success = false, message = "Bu yanıtı silme yetkiniz yok." });
                }
            }

            reply.IsActive = false;
            _context.ReviewReplies.Update(reply);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Yanıt başarıyla silindi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int reviewId, bool isLike = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return Json(new { success = false, message = "Yorum bulunamadı" });

            // Kendi yorumunu beğenemez
            if (review.UserId == userId)
                return Json(new { success = false, message = "Kendi yorumunuzu beğenemezsiniz" });

            var existingLike = await _context.ReviewLikes
                .FirstOrDefaultAsync(l => l.ReviewId == reviewId && l.UserId == userId);

            if (existingLike != null)
            {
                // Aynı tür beğeni ise kaldır, farklı ise değiştir
                if (existingLike.IsLike == isLike)
                {
                    _context.ReviewLikes.Remove(existingLike);
                }
                else
                {
                    existingLike.IsLike = isLike;
                    _context.ReviewLikes.Update(existingLike);
                }
            }
            else
            {
                var like = new ReviewLike
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    IsLike = isLike,
                    CreatedDate = DateTime.Now
                };
                _context.ReviewLikes.Add(like);
            }

            await _context.SaveChangesAsync();

            var likeCount = await _context.ReviewLikes.CountAsync(l => l.ReviewId == reviewId && l.IsLike);
            var dislikeCount = await _context.ReviewLikes.CountAsync(l => l.ReviewId == reviewId && !l.IsLike);

            return Json(new
            {
                success = true,
                likeCount = likeCount,
                dislikeCount = dislikeCount
            });
        }
    }
}