using System.Collections.Generic;
using Plafind.Models;

namespace Plafind.ViewModels
{
    public class BusinessDetailsViewModel
    {
        public Business Business { get; set; } = new();
        public List<Business> SimilarBusinesses { get; set; } = new();
    }
}

