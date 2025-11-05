using Microsoft.AspNetCore.Identity;

namespace AlanyaBusinessGuide.Models
{
    public class UserProfile
    {
        public int Id { get; set; } // Birincil anahtar
        public string? UserId { get; set; } // IdentityUser ile ilişkilendirme için
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public ApplicationUser? User { get; set; } // IdentityUser yerine ApplicationUser
    }
}