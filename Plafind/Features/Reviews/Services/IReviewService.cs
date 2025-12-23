using Plafind.Models;

namespace Plafind.Features.Reviews.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetBusinessReviewsAsync(int businessId, string? sortBy = "newest", int? minRating = null, int? maxRating = null, bool? withPhotosOnly = null);
        Task<Review?> GetReviewByIdAsync(int id);
        Task<Review> CreateReviewAsync(Review review, string userId, List<IFormFile>? images = null);
        Task<bool> UpdateBusinessRatingAsync(int businessId);
        Task<bool> LikeReviewAsync(int reviewId, string userId, bool isLike);
        Task<bool> CanUserLikeReviewAsync(int reviewId, string userId);
        Task<ReviewReply> ReplyToReviewAsync(int reviewId, string replyText, string userId);
        Task<bool> DeleteReplyAsync(int replyId, string userId, bool isAdmin);
        Task<Dictionary<int, bool>> GetUserLikesAsync(string userId, IEnumerable<int> reviewIds);
    }
}

