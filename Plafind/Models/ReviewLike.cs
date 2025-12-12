using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class ReviewLike
    {
        public int Id { get; set; }
        
        [Required]
        public int ReviewId { get; set; }
        public Review? Review { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public bool IsLike { get; set; } = true; // true = like, false = dislike
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
