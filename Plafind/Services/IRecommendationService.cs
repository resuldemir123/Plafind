using Plafind.Models;

namespace Plafind.Services
{
    public interface IRecommendationService
    {
        Task<RecommendationResult> GetPersonalizedRecommendationsAsync(string userId, string? timeOfDay = null, string? weather = null);
    }

    public class RecommendationResult
    {
        public string RecommendationText { get; set; } = string.Empty;
        public List<BusinessRecommendation> RecommendedBusinesses { get; set; } = new List<BusinessRecommendation>();
        public string Reasoning { get; set; } = string.Empty;
    }

    public class BusinessRecommendation
    {
        public int BusinessId { get; set; }
        public string BusinessName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public double? Rating { get; set; }
        public int TotalReviews { get; set; }
        public string? ImageUrl { get; set; }
        public string? PriceRange { get; set; }
    }
}

