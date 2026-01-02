using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Plafind.Models
{
    public class Review
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Nullable foreign key
        public int BusinessId { get; set; }
        public int? BranchId { get; set; } // Şube bilgisi (opsiyonel)
        public string? Comment { get; set; }
        public int Rating { get; set; }
        
        // Alt kategori puanları (1-5 arası)
        public int? ServiceRating { get; set; } // Hizmet puanı
        public int? TasteRating { get; set; } // Lezzet puanı
        public int? ValueRating { get; set; } // Fiyat/Performans puanı
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsApproved { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ApplicationUser? User { get; set; }
        public Business? Business { get; set; }
        public Branch? Branch { get; set; }
        public ICollection<ReviewReply> Replies { get; set; } = new List<ReviewReply>();
        public ICollection<ReviewLike> Likes { get; set; } = new List<ReviewLike>();
        public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
        
        // Computed properties (not stored in DB)
        [NotMapped]
        public int LikeCount { get; set; } = 0;
        
        [NotMapped]
        public int DislikeCount { get; set; } = 0;
        
        [NotMapped]
        public int HelpfulCount { get; set; } = 0; // Yararlı bulma sayısı
    }
}