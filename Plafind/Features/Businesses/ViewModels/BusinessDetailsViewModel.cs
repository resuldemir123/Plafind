using Plafind.Models;

namespace Plafind.Features.Businesses.ViewModels
{
    public class BusinessDetailsViewModel
    {
        public Business Business { get; set; } = new();
        public List<Business> SimilarBusinesses { get; set; } = new();
    }
}

