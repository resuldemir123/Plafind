using System;
using System.Collections.Generic;

namespace AlanyaBusinessGuide.Models
{
    public class Business
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }

        public bool IsActive { get; set; } = true;
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? WorkingHours { get; set; }
        public string? PriceRange { get; set; }
        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public bool IsApproved { get; set; } = false;

        public double AverageRating { get; set; } = 0;
        public int TotalReviews { get; set; } = 0;

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    }
}
