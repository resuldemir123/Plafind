namespace Plafind.Models
{
    /// <summary>
    /// Yorum raporlama sistemi
    /// </summary>
    public class ReviewReport
    {
        public int Id { get; set; }
        public int ReviewId { get; set; }
        public string? ReporterUserId { get; set; } // Raporlayan kullanıcı
        public string? Reason { get; set; } // Rapor nedeni
        public string? Description { get; set; } // Detaylı açıklama
        public ReviewReportStatus Status { get; set; } = ReviewReportStatus.Pending; // Durum
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ResolvedDate { get; set; }
        public string? ResolvedBy { get; set; } // Çözen admin
        public string? ResolutionNote { get; set; } // Çözüm notu
        
        // Navigation properties
        public Review? Review { get; set; }
        public ApplicationUser? ReporterUser { get; set; }
    }
    
    public enum ReviewReportStatus
    {
        Pending = 0,    // Beklemede
        UnderReview = 1, // İnceleniyor
        Resolved = 2,   // Çözüldü
        Dismissed = 3   // Reddedildi
    }
}

