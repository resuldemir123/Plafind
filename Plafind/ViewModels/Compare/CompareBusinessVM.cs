namespace Plafind.ViewModels.Compare
{
    public class CompareBusinessVM
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? PriceRange { get; set; }
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public string? ImageUrl { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? WorkingHours { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime CreatedDate { get; set; }
        
        // Analiz alanları
        public int PriceValue { get; set; }
        public double ValueScore { get; set; }
        public bool IsRecommended { get; set; }
        public int PriceRank { get; set; }
        public int RatingRank { get; set; }
        
        // Detaylar için
        public int ActiveReviewsCount { get; set; }
        public int ActiveImagesCount { get; set; }
        public int ActiveCampaignsCount { get; set; }
        public int UpcomingEventsCount { get; set; }
    }
}

