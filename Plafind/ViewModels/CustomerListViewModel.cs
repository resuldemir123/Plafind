using Plafind.Models;

namespace Plafind.ViewModels
{
    public class CustomerListViewModel
    {
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        public List<CustomerInfo> Customers { get; set; } = new List<CustomerInfo>();
        
        // Filtreleme
        public string? SearchTerm { get; set; }
        public string? SegmentFilter { get; set; }
        public string? SortBy { get; set; } // Name, LastVisit, TotalSpent, Rating
        
        // Sayfalama
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public int TotalCustomers { get; set; }
    }
    
    public class CustomerInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        
        // İstatistikler
        public int TotalReservations { get; set; }
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastVisitDate { get; set; }
        public DateTime? FirstVisitDate { get; set; }
        
        // Segmentasyon
        public string CustomerSegment { get; set; } = "Regular"; // VIP, Regular, New, AtRisk
        public int VisitFrequency { get; set; } // Son 30 günde kaç kez geldi
        
        // Son rezervasyon
        public Reservation? LastReservation { get; set; }
        
        // Son yorum
        public Review? LastReview { get; set; }
    }
    
    public class CustomerDetailViewModel
    {
        public ApplicationUser? Customer { get; set; }
        public int BusinessId { get; set; }
        public Business? Business { get; set; }
        
        public CustomerInfo? CustomerInfo { get; set; }
        public List<Reservation> Reservations { get; set; } = new List<Reservation>();
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<CustomerInteraction> Interactions { get; set; } = new List<CustomerInteraction>();
        public List<Message> Messages { get; set; } = new List<Message>();
        
        // İstatistikler
        public decimal LifetimeValue { get; set; }
        public int TotalVisits { get; set; }
        public double AverageSpending { get; set; }
        public string PreferredTime { get; set; } = string.Empty;
        public int PreferredPartySize { get; set; }
    }
}
