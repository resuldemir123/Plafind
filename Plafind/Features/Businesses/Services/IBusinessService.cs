using Plafind.Models;
using Plafind.Features.Businesses.ViewModels;

namespace Plafind.Features.Businesses.Services
{
    public interface IBusinessService
    {
        Task<IEnumerable<Business>> GetActiveBusinessesAsync();
        Task<BusinessDetailsViewModel?> GetBusinessDetailsAsync(int id);
        Task<IEnumerable<Business>> GetSimilarBusinessesAsync(int businessId, int categoryId, int count = 6);
        Task<Business?> GetBusinessByIdAsync(int id);
        Task<Business> CreateBusinessAsync(Business business, string userId, bool isAdmin);
        Task<Business> UpdateBusinessAsync(Business business);
        Task<bool> DeleteBusinessAsync(int id);
        Task<IEnumerable<object>> GetBusinessLocationsAsync();
        Task<BusinessListViewModel> GetBusinessesWithFiltersAsync(BusinessListViewModel filters);
    }
}

