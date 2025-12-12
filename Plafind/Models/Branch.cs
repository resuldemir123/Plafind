using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Branch
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string? Address { get; set; }
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        [StringLength(100)]
        public string? Email { get; set; }
        
        [StringLength(200)]
        public string? ManagerName { get; set; }
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [StringLength(500)]
        public string? WorkingHours { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        // Navigation properties
        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
