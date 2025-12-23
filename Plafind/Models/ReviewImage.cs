namespace Plafind.Models
{
    public class ReviewImage
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation property
        public Review? Review { get; set; }
    }
}

