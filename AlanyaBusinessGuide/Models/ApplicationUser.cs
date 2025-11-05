using Microsoft.AspNetCore.Identity;

namespace AlanyaBusinessGuide.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        // Navigation properties (initialize to avoid CS8618)
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
    }
}