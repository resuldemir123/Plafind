using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Plafind.Features.Reviews.Services
{
    public class ReviewService : IReviewService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReviewService(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IEnumerable<Review>> GetBusinessReviewsAsync(int businessId, string? sortBy = "newest", int? minRating = null, int? maxRating = null, bool? withPhotosOnly = null)
        {
            var query = _context.Reviews
                .Where(r => r.BusinessId == businessId && r.IsActive && r.IsApproved)
                .Include(r => r.User)
                .Include(r => r.Likes)
                .Include(r => r.Images)
                .AsQueryable();

            // Fotoğraflı yorumlar filtresi
            if (withPhotosOnly == true)
            {
                query = query.Where(r => r.Images.Any(i => i.IsActive));
            }

            // Rating filtreleme
            if (minRating.HasValue)
                query = query.Where(r => r.Rating >= minRating.Value);
            if (maxRating.HasValue)
                query = query.Where(r => r.Rating <= maxRating.Value);

            // Sıralama
            query = sortBy?.ToLower() switch
            {
                "highest" => query.OrderByDescending(r => r.Rating),
                "lowest" => query.OrderBy(r => r.Rating),
                "mostliked" => query.OrderByDescending(r => r.Likes.Count(l => l.IsLike)),
                "mosthelpful" => query.OrderByDescending(r => r.Likes.Count(l => l.IsLike)).ThenByDescending(r => r.CreatedDate),
                "oldest" => query.OrderBy(r => r.CreatedDate),
                _ => query.OrderByDescending(r => r.CreatedDate) // "newest"
            };

            var reviews = await query.ToListAsync();

            // Like/Dislike ve Helpful sayılarını hesapla
            foreach (var review in reviews)
            {
                review.LikeCount = review.Likes.Count(l => l.IsLike);
                review.DislikeCount = review.Likes.Count(l => !l.IsLike);
                review.HelpfulCount = review.LikeCount; // Yararlı bulma = like sayısı
            }

            return reviews;
        }

        public async Task<Review?> GetReviewByIdAsync(int id)
        {
            return await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Review> CreateReviewAsync(Review review, string userId, List<IFormFile>? images = null)
        {
            review.UserId = userId;
            review.CreatedDate = DateTime.Now;
            review.IsApproved = true; // Yorumlar otomatik onaylanır
            review.IsActive = true;

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Resimleri yükle
            if (images != null && images.Any())
            {
                await SaveReviewImagesAsync(review.Id, images);
            }

            // İşletme puanını güncelle
            await UpdateBusinessRatingAsync(review.BusinessId);

            return review;
        }

        private async Task SaveReviewImagesAsync(int reviewId, List<IFormFile> images)
        {
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "reviews");
            Directory.CreateDirectory(uploadsPath);

            var displayOrder = 0;
            foreach (var image in images)
            {
                if (image == null || image.Length == 0)
                    continue;

                // Dosya boyutu kontrolü (5MB)
                if (image.Length > 5 * 1024 * 1024)
                    continue;

                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(extension) || !IsSupportedImageExtension(extension))
                    continue;

                try
                {
                    var fileName = $"{reviewId}_{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    var reviewImage = new ReviewImage
                    {
                        ReviewId = reviewId,
                        ImageUrl = $"/uploads/reviews/{fileName}",
                        DisplayOrder = displayOrder++,
                        CreatedDate = DateTime.Now,
                        IsActive = true
                    };

                    _context.ReviewImages.Add(reviewImage);
                }
                catch
                {
                    // Hata durumunda devam et
                    continue;
                }
            }

            await _context.SaveChangesAsync();
        }

        private bool IsSupportedImageExtension(string extension)
        {
            var allowed = new[] { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
            return allowed.Contains(extension.ToLowerInvariant());
        }

        public async Task<bool> UpdateBusinessRatingAsync(int businessId)
        {
            var business = await _context.Businesses.FindAsync(businessId);
            if (business == null)
                return false;

            var allReviews = await _context.Reviews
                .Where(r => r.BusinessId == businessId && r.IsApproved && r.IsActive)
                .ToListAsync();

            business.AverageRating = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0;
            business.TotalReviews = allReviews.Count;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> LikeReviewAsync(int reviewId, string userId, bool isLike)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return false;

            // Kendi yorumunu beğenemez
            if (review.UserId == userId)
                return false;

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
            return true;
        }

        public async Task<bool> CanUserLikeReviewAsync(int reviewId, string userId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null)
                return false;

            return review.UserId != userId;
        }

        public async Task<ReviewReply> ReplyToReviewAsync(int reviewId, string replyText, string userId)
        {
            var review = await _context.Reviews
                .Include(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == reviewId);

            if (review == null)
                throw new ArgumentException("Yorum bulunamadı", nameof(reviewId));

            var reply = new ReviewReply
            {
                ReviewId = reviewId,
                UserId = userId,
                ReplyText = replyText,
                CreatedDate = DateTime.Now,
                IsActive = true,
                IsApproved = true
            };

            _context.ReviewReplies.Add(reply);
            await _context.SaveChangesAsync();
            return reply;
        }

        public async Task<bool> DeleteReplyAsync(int replyId, string userId, bool isAdmin)
        {
            var reply = await _context.ReviewReplies
                .Include(r => r.Review)
                .ThenInclude(r => r.Business)
                .FirstOrDefaultAsync(r => r.Id == replyId);

            if (reply == null)
                return false;

            // İşletme sahibi veya admin kontrolü
            if (!isAdmin && reply.Review?.Business?.OwnerId != userId)
                return false;

            reply.IsActive = false;
            _context.ReviewReplies.Update(reply);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Dictionary<int, bool>> GetUserLikesAsync(string userId, IEnumerable<int> reviewIds)
        {
            if (string.IsNullOrEmpty(userId) || !reviewIds.Any())
                return new Dictionary<int, bool>();

            return await _context.ReviewLikes
                .Where(l => l.UserId == userId && reviewIds.Contains(l.ReviewId))
                .ToDictionaryAsync(l => l.ReviewId, l => l.IsLike);
        }
    }
}

