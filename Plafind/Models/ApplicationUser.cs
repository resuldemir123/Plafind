using Microsoft.AspNetCore.Identity;

namespace Plafind.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public bool ConsentAccepted { get; set; } = false;
        public DateTime? ConsentDate { get; set; }
        // PhoneNumber zaten IdentityUser'da var, ama açıkça belirtmek için

        // Navigation properties (initialize to avoid CS8618)
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<UserPhoto> Photos { get; set; } = new List<UserPhoto>();
    }
}