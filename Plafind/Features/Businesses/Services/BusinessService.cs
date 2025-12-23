using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Features.Businesses.ViewModels;
using Plafind.Models;
using Plafind.Features.Businesses.Services;
using System.Text.Json;

namespace Plafind.Features.Businesses.Services
{
    public class BusinessService : IBusinessService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILocationService _locationService;

        public BusinessService(ApplicationDbContext context, ILocationService locationService)
        {
            _context = context;
            _locationService = locationService;
        }

        public async Task<IEnumerable<Business>> GetActiveBusinessesAsync()
        {
            return await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .Include(b => b.Favorites)
                .ToListAsync();
        }

        public async Task<BusinessDetailsViewModel?> GetBusinessDetailsAsync(int id)
        {
            var business = await _context.Businesses
                .Include(b => b.Category)
                .Include(b => b.Reviews.Where(r => r.IsActive && r.IsApproved))
                    .ThenInclude(r => r.User)
                .Include(b => b.Favorites)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (business == null)
                return null;

            // ViewCount'u artır
            business.ViewCount = (business.ViewCount ?? 0) + 1;
            _context.Businesses.Update(business);
            await _context.SaveChangesAsync();

            var similarBusinesses = new List<Business>();
            if (business.CategoryId.HasValue)
            {
                similarBusinesses = (await GetSimilarBusinessesAsync(id, business.CategoryId.Value, 6)).ToList();
            }

            return new BusinessDetailsViewModel
            {
                Business = business,
                SimilarBusinesses = similarBusinesses
            };
        }

        public async Task<IEnumerable<Business>> GetSimilarBusinessesAsync(int businessId, int categoryId, int count = 6)
        {
            return await _context.Businesses
                .Where(b => b.Id != businessId &&
                            b.CategoryId == categoryId &&
                            b.IsActive &&
                            b.IsApproved)
                .Include(b => b.Category)
                .OrderByDescending(b => b.IsFeatured)
                .ThenByDescending(b => b.AverageRating)
                .ThenByDescending(b => b.CreatedDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Business?> GetBusinessByIdAsync(int id)
        {
            return await _context.Businesses
                .Include(b => b.Category)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Business> CreateBusinessAsync(Business business, string userId, bool isAdmin)
        {
            business.IsApproved = isAdmin;
            business.IsActive = true;
            business.CreatedBy = userId;
            business.CreatedDate = DateTime.Now;

            _context.Add(business);
            await _context.SaveChangesAsync();
            return business;
        }

        public async Task<Business> UpdateBusinessAsync(Business business)
        {
            business.UpdatedDate = DateTime.Now;
            _context.Update(business);
            await _context.SaveChangesAsync();
            return business;
        }

        public async Task<bool> DeleteBusinessAsync(int id)
        {
            var business = await _context.Businesses.FindAsync(id);
            if (business == null)
                return false;

            _context.Businesses.Remove(business);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetBusinessLocationsAsync()
        {
            return await _context.Businesses
                .Where(b => b.IsActive && b.IsApproved &&
                           (b.Latitude.HasValue && b.Longitude.HasValue || !string.IsNullOrEmpty(b.Address)))
                .Include(b => b.Category)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.Address,
                    Category = b.Category != null ? b.Category.Name : null,
                    b.Phone,
                    b.ImageUrl,
                    b.AverageRating,
                    b.TotalReviews,
                    Latitude = b.Latitude,
                    Longitude = b.Longitude,
                    HasCoordinates = b.Latitude.HasValue && b.Longitude.HasValue
                })
                .ToListAsync();
        }

        public async Task<BusinessListViewModel> GetBusinessesWithFiltersAsync(BusinessListViewModel filters)
        {
            var query = _context.Businesses
                .Where(b => b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .Include(b => b.Reviews)
                .AsQueryable();

            // Text search
            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var searchTerm = filters.SearchQuery.ToLower();
                query = query.Where(b =>
                    (!string.IsNullOrEmpty(b.Name) && b.Name.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(b.Description) && b.Description.ToLower().Contains(searchTerm)) ||
                    (!string.IsNullOrEmpty(b.Address) && b.Address.ToLower().Contains(searchTerm)));
            }

            // Category filter
            if (filters.CategoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == filters.CategoryId.Value);
            }

            // Rating filters
            if (filters.MinRating.HasValue)
            {
                query = query.Where(b => b.AverageRating >= filters.MinRating.Value);
            }
            if (filters.MaxRating.HasValue)
            {
                query = query.Where(b => b.AverageRating <= filters.MaxRating.Value);
            }

            // Price range filter
            if (!string.IsNullOrWhiteSpace(filters.PriceRange))
            {
                query = query.Where(b => b.PriceRange == filters.PriceRange);
            }

            // Features filter (JSON içinde arama)
            if (filters.Features.Any())
            {
                foreach (var feature in filters.Features)
                {
                    query = query.Where(b => b.FeaturesJson != null && b.FeaturesJson.Contains(feature));
                }
            }

            // Sorting
            query = filters.SortBy?.ToLower() switch
            {
                "rating" => query.OrderByDescending(b => b.AverageRating).ThenByDescending(b => b.TotalReviews),
                "newest" => query.OrderByDescending(b => b.CreatedDate),
                "reviews" => query.OrderByDescending(b => b.TotalReviews).ThenByDescending(b => b.AverageRating),
                "distance" when filters.NearMe == true && filters.UserLatitude.HasValue && filters.UserLongitude.HasValue =>
                    query.OrderBy(b => CalculateDistance(
                        filters.UserLatitude!.Value, filters.UserLongitude!.Value,
                        b.Latitude ?? 0, b.Longitude ?? 0)),
                _ => query.OrderByDescending(b => b.IsFeatured)
                    .ThenByDescending(b => b.AverageRating)
                    .ThenByDescending(b => b.TotalReviews)
            };

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Pagination
            var businesses = await query
                .Skip((filters.Page - 1) * filters.PageSize)
                .Take(filters.PageSize)
                .ToListAsync();

            // Parse Features JSON for each business
            foreach (var business in businesses)
            {
                if (!string.IsNullOrEmpty(business.FeaturesJson))
                {
                    try
                    {
                        business.Features = JsonSerializer.Deserialize<List<BusinessFeature>>(business.FeaturesJson);
                    }
                    catch
                    {
                        business.Features = new List<BusinessFeature>();
                    }
                }
            }

            // Get categories for dropdown
            var categories = await _context.Categories.ToListAsync();

            // Mesafe hesaplama
            var distances = new Dictionary<int, double>();
            if (filters.NearMe == true && filters.UserLatitude.HasValue && filters.UserLongitude.HasValue)
            {
                foreach (var business in businesses)
                {
                    if (business.Latitude.HasValue && business.Longitude.HasValue)
                    {
                        var distance = _locationService.CalculateDistance(
                            filters.UserLatitude.Value,
                            filters.UserLongitude.Value,
                            business.Latitude.Value,
                            business.Longitude.Value
                        );
                        distances[business.Id] = distance;
                    }
                }
            }

            return new BusinessListViewModel
            {
                Businesses = businesses,
                TotalCount = totalCount,
                Page = filters.Page,
                PageSize = filters.PageSize,
                SearchQuery = filters.SearchQuery,
                CategoryId = filters.CategoryId,
                MinRating = filters.MinRating,
                MaxRating = filters.MaxRating,
                PriceRange = filters.PriceRange,
                Features = filters.Features,
                SortBy = filters.SortBy ?? "featured",
                Categories = categories,
                PriceRanges = filters.PriceRanges,
                AvailableFeatures = filters.AvailableFeatures,
                NearMe = filters.NearMe,
                UserLatitude = filters.UserLatitude,
                UserLongitude = filters.UserLongitude,
                BusinessDistances = distances
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
}

