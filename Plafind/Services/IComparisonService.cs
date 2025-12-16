using System.Collections.Generic;
using System.Threading.Tasks;
using Plafind.Models;

namespace Plafind.Services
{
    /// <summary>
    /// İşletme karşılaştırma servisi interface'i
    /// </summary>
    public interface IComparisonService
    {
        /// <summary>
        /// Verilen ID'lere göre işletmeleri veritabanından çeker
        /// </summary>
        Task<List<Business>> GetBusinessesForComparisonAsync(List<int> businessIds);

        /// <summary>
        /// İşletmelerin özelliklerini normalleştirir ve karşılaştırma matrisi oluşturur
        /// </summary>
        ComparisonViewModel CreateComparisonMatrix(List<Business> businesses);
    }
}

