using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Plafind.Features.Reviews.Services;
using Plafind.Features.Reviews.ViewModels;
using Plafind.Features.Reviews.Mappings;
using Plafind.Data;
using Plafind.Models;
using AutoMapper;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Plafind.Features.Reviews.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public ReviewsController(
            IReviewService reviewService,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IMapper mapper)
        {
            _reviewService = reviewService;
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int businessId, string? sortBy = "newest", int? minRating = null, int? maxRating = null, bool? withPhotosOnly = null)
        {
            var reviews = await _reviewService.GetBusinessReviewsAsync(businessId, sortBy, minRating, maxRating, withPhotosOnly);

            // Kullanıcının beğenilerini al
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId) && reviews.Any())
            {
                var reviewIds = reviews.Select(r => r.Id);
                var userLikes = await _reviewService.GetUserLikesAsync(userId, reviewIds);
                ViewBag.UserLikes = userLikes;
            }

            ViewBag.BusinessId = businessId;
            ViewBag.SortBy = sortBy;
            ViewBag.MinRating = minRating;
            ViewBag.MaxRating = maxRating;
            ViewBag.WithPhotosOnly = withPhotosOnly;
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
        public async Task<IActionResult> Create(CreateReviewViewModel viewModel)
        {
            if (User.IsInRole("Admin"))
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.BusinessId = viewModel.BusinessId;
                return View(viewModel);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var review = _mapper.Map<Review>(viewModel);
            await _reviewService.CreateReviewAsync(review, user.Id, viewModel.Images);

            TempData["SuccessMessage"] = "Yorumunuz başarıyla eklendi.";
            return RedirectToAction("Details", "Businesses", new { id = viewModel.BusinessId });
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // İşletme sahibi kontrolü
            if (User.IsInRole("BusinessOwner"))
            {
                var review = await _reviewService.GetReviewByIdAsync(reviewId);
                if (review?.Business?.OwnerId != user.Id)
                {
                    return Json(new { success = false, message = "Bu yoruma yanıt verme yetkiniz yok." });
                }
            }

            try
            {
                var reply = await _reviewService.ReplyToReviewAsync(reviewId, replyText, user.Id);
                return Json(new { success = true, message = "Yanıt başarıyla eklendi.", replyId = reply.Id });
            }
            catch (ArgumentException)
            {
                return Json(new { success = false, message = "Yorum bulunamadı." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "BusinessOwner,Admin")]
        public async Task<IActionResult> DeleteReply(int replyId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var isAdmin = User.IsInRole("Admin");
            var result = await _reviewService.DeleteReplyAsync(replyId, user.Id, isAdmin);

            if (!result)
            {
                return Json(new { success = false, message = "Yanıt bulunamadı veya silme yetkiniz yok." });
            }

            return Json(new { success = true, message = "Yanıt başarıyla silindi." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int reviewId, bool isLike = true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Giriş yapmanız gerekiyor" });

            var canLike = await _reviewService.CanUserLikeReviewAsync(reviewId, userId);
            if (!canLike)
            {
                return Json(new { success = false, message = "Kendi yorumunuzu beğenemezsiniz" });
            }

            var result = await _reviewService.LikeReviewAsync(reviewId, userId, isLike);
            if (!result)
            {
                return Json(new { success = false, message = "Yorum bulunamadı" });
            }

            // Like/Dislike sayılarını hesapla
            var review = await _reviewService.GetReviewByIdAsync(reviewId);
            if (review != null)
            {
                var reviews = new[] { review };
                var userLikes = await _reviewService.GetUserLikesAsync(userId, reviews.Select(r => r.Id));
                
                review.LikeCount = review.Likes.Count(l => l.IsLike);
                review.DislikeCount = review.Likes.Count(l => !l.IsLike);
            }

            var likeCount = review?.Likes.Count(l => l.IsLike) ?? 0;
            var dislikeCount = review?.Likes.Count(l => !l.IsLike) ?? 0;

            return Json(new
            {
                success = true,
                likeCount = likeCount,
                dislikeCount = dislikeCount
            });
        }
    }
}

