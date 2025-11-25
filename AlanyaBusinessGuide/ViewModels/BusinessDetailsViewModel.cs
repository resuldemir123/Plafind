using System.Collections.Generic;
using AlanyaBusinessGuide.Models;

namespace AlanyaBusinessGuide.ViewModels
{
    public class BusinessDetailsViewModel
    {
        public Business Business { get; set; } = new();
        public List<Business> SimilarBusinesses { get; set; } = new();
    }
}

