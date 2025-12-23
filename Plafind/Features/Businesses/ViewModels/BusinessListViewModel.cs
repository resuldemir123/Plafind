using Plafind.Models;

namespace Plafind.Features.Businesses.ViewModels
{
    public class BusinessListViewModel
    {
        public IEnumerable<Business> Businesses { get; set; } = new List<Business>();
        public int TotalCount { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        
        // Filters
        public string? SearchQuery { get; set; }
        public int? CategoryId { get; set; }
        public double? MinRating { get; set; }
        public double? MaxRating { get; set; }
        public string? PriceRange { get; set; }
        public bool? IsOpen { get; set; }
        public bool? NearMe { get; set; }
        public double? UserLatitude { get; set; }
        public double? UserLongitude { get; set; }
        public List<string> Features { get; set; } = new();
        
        // Distance (computed)
        public Dictionary<int, double> BusinessDistances { get; set; } = new();
        
        // Sort
        public string SortBy { get; set; } = "featured"; // featured, rating, newest, reviews, distance
        
        // Available options for dropdowns
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public List<string> PriceRanges { get; set; } = new() { "$", "$$", "$$$", "$$$$" };
        public List<string> AvailableFeatures { get; set; } = new() 
        { 
            "WiFi", "Otopark", "Çocuk Dostu", "Teras", "Deniz Manzarası", 
            "Canlı Müzik", "Kahvaltı", "Özel Menü", "Glutensiz", "Vegan" 
        };
    }
}

