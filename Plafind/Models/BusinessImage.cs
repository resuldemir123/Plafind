using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class BusinessImage
    {
        public int Id { get; set; }
        
        [Required]
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Title { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public int DisplayOrder { get; set; } = 0;
        
        public bool IsPrimary { get; set; } = false;
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        [StringLength(100)]
        public string? CreatedBy { get; set; }
    }
}
