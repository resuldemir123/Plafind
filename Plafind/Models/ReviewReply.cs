using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class ReviewReply
    {
        public int Id { get; set; }
        
        [Required]
        public int ReviewId { get; set; }
        public Review? Review { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty; // İşletme sahibi veya admin
        public ApplicationUser? User { get; set; }
        
        [Required]
        public string ReplyText { get; set; } = string.Empty;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = true; // İşletme sahipleri için otomatik onay
    }
}
