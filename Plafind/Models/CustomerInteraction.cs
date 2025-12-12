using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class CustomerInteraction
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public ApplicationUser? Customer { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InteractionType { get; set; } = string.Empty; 
        // Types: Reservation, Review, Message, Call, Visit, Complaint, Compliment
        
        [StringLength(200)]
        public string? Subject { get; set; }
        
        [StringLength(2000)]
        public string? Notes { get; set; }
        
        public DateTime InteractionDate { get; set; } = DateTime.Now;
        
        [StringLength(50)]
        public string? Status { get; set; } // Completed, Pending, FollowUp
        
        public int? RelatedReservationId { get; set; }
        public Reservation? RelatedReservation { get; set; }
        
        public int? RelatedReviewId { get; set; }
        public Review? RelatedReview { get; set; }
        
        public int? RelatedMessageId { get; set; }
        public Message? RelatedMessage { get; set; }
        
        // İşletme sahibi veya çalışan tarafından eklenen notlar
        [StringLength(1000)]
        public string? InternalNotes { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; } // UserId of the person who created this interaction record
    }
}
