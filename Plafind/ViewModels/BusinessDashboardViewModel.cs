using Plafind.Models;

namespace Plafind.ViewModels
{
    public class BusinessDashboardViewModel
    {
        public Business? Business { get; set; }
        
        // Genel İstatistikler
        public int TotalReservations { get; set; }
        public int PendingReservations { get; set; }
        public int ConfirmedReservations { get; set; }
        public int CancelledReservations { get; set; }
        
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public int PositiveReviews { get; set; } // 4-5 yıldız
        public int NegativeReviews { get; set; } // 1-2 yıldız
        
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int ReturningCustomers { get; set; }
        
        // Gelir İstatistikleri
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueLastMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        
        // Grafik Verileri
        public List<MonthlyRevenue> MonthlyRevenues { get; set; } = new List<MonthlyRevenue>();
        public List<DailyReservation> DailyReservations { get; set; } = new List<DailyReservation>();
        public List<RatingDistribution> RatingDistributions { get; set; } = new List<RatingDistribution>();
        public List<CategoryRevenue> CategoryRevenues { get; set; } = new List<CategoryRevenue>();
        
        // Son Aktiviteler
        public List<Reservation> RecentReservations { get; set; } = new List<Reservation>();
        public List<Review> RecentReviews { get; set; } = new List<Review>();
        
        // Şube İstatistikleri (varsa)
        public int TotalBranches { get; set; }
        public List<BranchStats> BranchStatistics { get; set; } = new List<BranchStats>();
    }
    
    public class MonthlyRevenue
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int ReservationCount { get; set; }
    }
    
    public class DailyReservation
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    
    public class RatingDistribution
    {
        public int Rating { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
    
    public class CategoryRevenue
    {
        public string Category { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
    
    public class BranchStats
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = string.Empty;
        public int ReservationCount { get; set; }
        public decimal Revenue { get; set; }
        public double AverageRating { get; set; }
    }
}
