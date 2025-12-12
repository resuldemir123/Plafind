using System;
using System.ComponentModel.DataAnnotations;

namespace Plafind.Models
{
    public class Campaign
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
        
        [StringLength(50)]
        public string CampaignType { get; set; } = "Discount"; // Discount, Promotion, SpecialOffer
        
        public decimal? DiscountAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        
        [StringLength(50)]
        public string? CouponCode { get; set; }
        
        [Required]
        public DateTime StartDate { get; set; }
        
        [Required]
        public DateTime EndDate { get; set; }
        
        [StringLength(500)]
        public string? ImageUrl { get; set; }
        
        public int? MaxUses { get; set; }
        public int CurrentUses { get; set; } = 0;
        
        public int? MaxUsesPerUser { get; set; } = 1;
        
        public decimal? MinimumPurchaseAmount { get; set; }
        
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        
        [StringLength(100)]
        public string? CreatedBy { get; set; }
        
        public ICollection<CampaignUsage> Usages { get; set; } = new List<CampaignUsage>();
    }

    public class CampaignUsage
    {
        public int Id { get; set; }
        
        [Required]
        public int CampaignId { get; set; }
        public Campaign? Campaign { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser? User { get; set; }
        
        public DateTime UsedDate { get; set; } = DateTime.Now;
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        public decimal? DiscountApplied { get; set; }
    }
}
