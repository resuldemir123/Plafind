using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Event
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public DateTime StartDate { get; set; }
        
        public DateTime? EndDate { get; set; }
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(500)]
        public string? ImageUrl { get; set; }
        
        [StringLength(100)]
        public string? EventType { get; set; } // Concert, Workshop, Promotion, etc.
        
        public decimal? Price { get; set; }
        
        [StringLength(10)]
        public string? Currency { get; set; } = "TRY";
        
        public int? MaxAttendees { get; set; }
        public int CurrentAttendees { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        
        public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    }

    public class EventAttendee
    {
        public int Id { get; set; }
        
        [Required]
        public int EventId { get; set; }
        public Event? Event { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public DateTime RegisteredDate { get; set; } = DateTime.Now;
        
        public bool IsConfirmed { get; set; } = false;
        public DateTime? ConfirmedDate { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
