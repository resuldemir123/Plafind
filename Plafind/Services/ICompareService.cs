using Microsoft.AspNetCore.Http;
using Plafind.Data;
using Plafind.ViewModels.Compare;

namespace Plafind.Services
{
    /// <summary>
    /// Karşılaştırma servisi interface'i - Session ve analiz yönetimi
    /// </summary>
    public interface ICompareService
    {
        /// <summary>
        /// Session'dan karşılaştırma listesini alır
        /// </summary>
        List<int> GetCompareList(ISession session, string? userId = null);

        /// <summary>
        /// Session'a karşılaştırma listesini kaydeder
        /// </summary>
        void SaveCompareList(ISession session, List<int> businessIds, string? userId = null);

        /// <summary>
        /// İşletmeyi karşılaştırma listesine ekler (kategori kontrolü ile)
        /// </summary>
        (bool success, string message, int count) AddToCompareList(ISession session, int businessId, int? existingCategoryId = null, string? userId = null);

        /// <summary>
        /// İşletmeyi karşılaştırma listesinden kaldırır
        /// </summary>
        (bool success, string message, int count) RemoveFromCompareList(ISession session, int businessId, string? userId = null);

        /// <summary>
        /// Karşılaştırma listesini temizler
        /// </summary>
        void ClearCompareList(ISession session, string? userId = null);

        /// <summary>
        /// Karşılaştırma listesi sayısını döner
        /// </summary>
        int GetCompareListCount(ISession session, string? userId = null);

        /// <summary>
        /// İşletme karşılaştırma listesinde mi kontrol eder
        /// </summary>
        bool IsInCompareList(ISession session, int businessId, string? userId = null);

        /// <summary>
        /// Fiyat aralığını sayısal değere dönüştürür ($ = 1, $$ = 2, $$$ = 3, $$$$ = 4)
        /// </summary>
        int GetPriceRangeValue(string? priceRange);

        /// <summary>
        /// Fiyat/Puan değer skoru hesaplar (düşük fiyat + yüksek puan = iyi skor)
        /// </summary>
        double GetValueScore(int priceValue, double rating);

        /// <summary>
        /// Karşılaştırma analizini hazırlar (ViewModel doldurur)
        /// </summary>
        Task<CompareIndexVM> PrepareComparisonAnalysisAsync(
            ApplicationDbContext context, 
            List<CompareBusinessVM> businessVMs);

        /// <summary>
        /// Kategori uyumluluğunu kontrol eder
        /// </summary>
        CategoryCompatibilityVM AnalyzeCategoryCompatibility(List<string> categories);
    }
}

