using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Message
    {
        public int Id { get; set; }
        
        [Required]
        public string SenderId { get; set; } = string.Empty;
        public ApplicationUser? Sender { get; set; }
        
        [Required]
        public string ReceiverId { get; set; } = string.Empty;
        public ApplicationUser? Receiver { get; set; }
        
        [Required]
        public string Subject { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public bool IsDeletedBySender { get; set; } = false;
        public bool IsDeletedByReceiver { get; set; } = false;
        
        public int? RelatedBusinessId { get; set; }
        public Business? RelatedBusiness { get; set; }
        
        public int? RelatedReservationId { get; set; }
        public Reservation? RelatedReservation { get; set; }
    }

    public class Conversation
    {
        public int Id { get; set; }
        
        [Required]
        public string User1Id { get; set; } = string.Empty;
        public ApplicationUser? User1 { get; set; }
        
        [Required]
        public string User2Id { get; set; } = string.Empty;
        public ApplicationUser? User2 { get; set; }
        
        public DateTime LastMessageDate { get; set; } = DateTime.Now;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public bool IsArchivedByUser1 { get; set; } = false;
        public bool IsArchivedByUser2 { get; set; } = false;
    }
}
