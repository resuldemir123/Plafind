using Microsoft.AspNetCore.Identity;

namespace Plafind.Models
{
    public class UserFavorite
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Nullable foreign key
        public int BusinessId { get; set; }
        public DateTime AddedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Business? Business { get; set; }
    }
}