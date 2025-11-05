using Microsoft.AspNetCore.Identity;

namespace AlanyaBusinessGuide.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Nullable foreign key
        public int BusinessId { get; set; }
        public string? Comment { get; set; }
        public int Rating { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Business? Business { get; set; }
    }
}