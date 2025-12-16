using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using Plafind.Models;
using Plafind.ViewModels.Compare;
using System.Text.Json;

namespace Plafind.Services
{
    /// <summary>
    /// Karşılaştırma servisi - Session yönetimi ve analiz hesaplamaları
    /// </summary>
    public class CompareService : ICompareService
    {
        private const string CompareSessionKey = "CompareBusinesses";
        private readonly ILogger<CompareService> _logger;

        public CompareService(ILogger<CompareService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<int> GetCompareList(ISession session)
        {
            var compareJson = session.GetString(CompareSessionKey);
            if (string.IsNullOrEmpty(compareJson))
                return new List<int>();

            try
            {
                return JsonSerializer.Deserialize<List<int>>(compareJson) ?? new List<int>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Karşılaştırma listesi deserialize edilemedi");
                return new List<int>();
            }
        }

        public void SaveCompareList(ISession session, List<int> businessIds)
        {
            try
            {
                var compareJson = JsonSerializer.Serialize(businessIds);
                session.SetString(CompareSessionKey, compareJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Karşılaştırma listesi kaydedilemedi");
            }
        }

        public (bool success, string message, int count) AddToCompareList(
            ISession session, 
            int businessId, 
            int? existingCategoryId = null)
        {
            var compareIds = GetCompareList(session);

            if (compareIds.Contains(businessId))
            {
                return (false, "Bu işletme zaten karşılaştırma listesinde.", compareIds.Count);
            }

            if (compareIds.Count >= 4)
            {
                return (false, "En fazla 4 işletme karşılaştırabilirsiniz.", compareIds.Count);
            }

            // Kategori kontrolü: Eğer listede işletme varsa, aynı kategoride olmalı
            // Bu kontrolü Controller'da yapacağız çünkü DB'ye erişim gerekiyor
            // Burada sadece temel validasyonları yapıyoruz

            compareIds.Add(businessId);
            SaveCompareList(session, compareIds);

            return (true, "İşletme karşılaştırmaya eklendi.", compareIds.Count);
        }

        public (bool success, string message, int count) RemoveFromCompareList(ISession session, int businessId)
        {
            var compareIds = GetCompareList(session);
            
            if (!compareIds.Contains(businessId))
            {
                return (false, "Bu işletme karşılaştırma listesinde bulunamadı.", compareIds.Count);
            }

            compareIds.Remove(businessId);
            SaveCompareList(session, compareIds);

            return (true, "İşletme karşılaştırmadan kaldırıldı.", compareIds.Count);
        }

        public void ClearCompareList(ISession session)
        {
            session.Remove(CompareSessionKey);
        }

        public int GetCompareListCount(ISession session)
        {
            return GetCompareList(session).Count;
        }

        public bool IsInCompareList(ISession session, int businessId)
        {
            return GetCompareList(session).Contains(businessId);
        }

        public int GetPriceRangeValue(string? priceRange)
        {
            if (string.IsNullOrWhiteSpace(priceRange))
                return 0;

            // "$", "$$", "$$$", "$$$$" bekleniyor. Diğer durumda uzunluk bazlı.
            return priceRange.Count(c => c == '$');
        }

        public double GetValueScore(int priceValue, double rating)
        {
            // Rating 0 ise skor 0
            if (rating <= 0)
                return 0;

            // 0 fiyat bilgisi varsa daha düşük ağırlık ver
            if (priceValue <= 0)
                return rating;

            // Düşük fiyat + yüksek puan iyi: (6 - fiyat)*puan
            return (6 - priceValue) * rating;
        }

        public CategoryCompatibilityVM AnalyzeCategoryCompatibility(List<string> categories)
        {
            var distinctCategories = categories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .ToList();

            return new CategoryCompatibilityVM
            {
                AllSameCategory = distinctCategories.Count == 1,
                CommonCategoryName = distinctCategories.Count == 1 ? distinctCategories.First() : null,
                AllCategories = distinctCategories
            };
        }

        public async Task<CompareIndexVM> PrepareComparisonAnalysisAsync(
            ApplicationDbContext context,
            List<CompareBusinessVM> businessVMs)
        {
            var viewModel = new CompareIndexVM();

            if (!businessVMs.Any())
            {
                viewModel.EmptyMessage = "Karşılaştırma listenizdeki işletmeler bulunamadı.";
                return viewModel;
            }

            // Fiyat sıralaması (artarak: 1=en ucuz)
            var priceSorted = businessVMs.OrderBy(b => b.PriceValue).ThenBy(b => b.Id).ToList();
            for (int i = 0; i < priceSorted.Count; i++)
            {
                priceSorted[i].PriceRank = i + 1;
            }

            // Puan sıralaması (azalarak: 1=en yüksek)
            var ratingSorted = businessVMs.OrderByDescending(b => b.AverageRating).ThenBy(b => b.Id).ToList();
            for (int i = 0; i < ratingSorted.Count; i++)
            {
                ratingSorted[i].RatingRank = i + 1;
            }

            // En iyi değer (ValueScore en yüksek)
            var bestValueBusiness = businessVMs.OrderByDescending(b => b.ValueScore).FirstOrDefault();
            if (bestValueBusiness != null)
            {
                bestValueBusiness.IsRecommended = true;
            }

            // Summary hesapla
            viewModel.Summary = new CompareSummaryVM
            {
                BestRatingBusinessName = businessVMs.OrderByDescending(b => b.AverageRating).FirstOrDefault()?.Name ?? "N/A",
                BestRatingValue = businessVMs.Any() ? businessVMs.Max(b => b.AverageRating) : 0,
                BestValueBusinessName = bestValueBusiness?.Name ?? "N/A",
                BestValueScore = bestValueBusiness?.ValueScore ?? 0,
                AverageRating = businessVMs.Any() ? businessVMs.Average(b => b.AverageRating) : 0,
                PriceDistribution = businessVMs
                    .GroupBy(b => b.PriceRange ?? "Belirtilmemiş")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Kategori uyumluluğu
            var categories = businessVMs
                .Where(b => !string.IsNullOrWhiteSpace(b.CategoryName))
                .Select(b => b.CategoryName!)
                .ToList();

            viewModel.CategoryInfo = AnalyzeCategoryCompatibility(categories);

            // Grafik dataları
            viewModel.Labels = businessVMs.Select(b => b.Name ?? "İsimsiz").ToList();
            viewModel.Ratings = businessVMs.Select(b => b.AverageRating).ToList();
            viewModel.PriceValues = businessVMs.Select(b => b.PriceValue).ToList();
            viewModel.ValueScores = businessVMs.Select(b => b.ValueScore).ToList();

            viewModel.Items = businessVMs;

            return viewModel;
        }
    }
}

