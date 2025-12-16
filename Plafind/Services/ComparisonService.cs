using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Services
{
    /// <summary>
    /// İşletme karşılaştırma servisi implementasyonu
    /// </summary>
    public class ComparisonService : IComparisonService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ComparisonService> _logger;

        public ComparisonService(ApplicationDbContext context, ILogger<ComparisonService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Verilen ID'lere göre işletmeleri veritabanından çeker
        /// </summary>
        public async Task<List<Business>> GetBusinessesForComparisonAsync(List<int> businessIds)
        {
            if (businessIds == null || !businessIds.Any())
            {
                _logger.LogWarning("GetBusinessesForComparisonAsync: Boş ID listesi gönderildi");
                return new List<Business>();
            }

            // Maksimum 4 işletme limiti
            var limitedIds = businessIds.Distinct().Take(4).ToList();

            var businesses = await _context.Businesses
                .Where(b => limitedIds.Contains(b.Id) && b.IsActive && b.IsApproved)
                .Include(b => b.Category)
                .ToListAsync();

            // FeaturesJson'dan Features'a dönüştür (NotMapped property için)
            foreach (var business in businesses)
            {
                if (!string.IsNullOrWhiteSpace(business.FeaturesJson))
                {
                    business.Features = DeserializeFeatures(business.FeaturesJson);
                }
                else
                {
                    business.Features = new List<BusinessFeature>();
                }
            }

            _logger.LogInformation("GetBusinessesForComparisonAsync: {Count} işletme bulundu", businesses.Count);
            return businesses;
        }

        /// <summary>
        /// İşletmelerin özelliklerini normalleştirir ve karşılaştırma matrisi oluşturur
        /// </summary>
        public ComparisonViewModel CreateComparisonMatrix(List<Business> businesses)
        {
            if (businesses == null || !businesses.Any())
            {
                _logger.LogWarning("CreateComparisonMatrix: Boş işletme listesi");
                return new ComparisonViewModel { Businesses = new List<Business>() };
            }

            var viewModel = new ComparisonViewModel
            {
                Businesses = businesses
            };

            // 1. Tüm özellikleri normalleştir ve master liste oluştur
            var allFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var business in businesses)
            {
                var normalizedFeatures = BusinessFeatureDictionary.NormalizeBusinessFeatures(business.Features);
                
                foreach (var feature in normalizedFeatures)
                {
                    if (!allFeatures.ContainsKey(feature.Name))
                    {
                        allFeatures[feature.Name] = feature.Name;
                    }
                }
            }

            // 2. Özellikleri öncelik sırasına göre sırala
            var sortedFeatureNames = BusinessFeatureDictionary.SortFeaturesByPriority(allFeatures.Keys);

            // 3. Kritik özellikleri ayır (isim, fiyat, rating)
            var criticalFeatureRows = CreateCriticalFeatureRows(businesses);
            viewModel.CriticalFeatures = criticalFeatureRows;

            // 4. Dinamik özellikler için satırlar oluştur
            var featureRows = new List<ComparisonFeatureRow>();

            foreach (var featureName in sortedFeatureNames)
            {
                var row = new ComparisonFeatureRow
                {
                    AttributeName = featureName,
                    Values = new List<string>()
                };

                // Her işletme için bu özelliğin değerini bul
                foreach (var business in businesses)
                {
                    var normalizedFeatures = BusinessFeatureDictionary.NormalizeBusinessFeatures(business.Features);
                    var feature = normalizedFeatures.FirstOrDefault(f => 
                        f.Name.Equals(featureName, StringComparison.OrdinalIgnoreCase));

                    if (feature != null && !string.IsNullOrWhiteSpace(feature.Value))
                    {
                        row.Values.Add(feature.Value);
                    }
                    else
                    {
                        row.Values.Add("N/A");
                    }
                }

                // Farklılık kontrolü
                row.IsDifferent = row.Values.Distinct().Count() > 1;

                featureRows.Add(row);
            }

            viewModel.FeatureRows = featureRows;

            _logger.LogInformation("CreateComparisonMatrix: {FeatureCount} özellik satırı oluşturuldu", featureRows.Count);
            return viewModel;
        }

        /// <summary>
        /// Kritik özellikler için satırlar oluşturur (isim, fiyat, rating)
        /// </summary>
        private List<ComparisonFeatureRow> CreateCriticalFeatureRows(List<Business> businesses)
        {
            var criticalRows = new List<ComparisonFeatureRow>();

            // İşletme Adı
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "İşletme Adı",
                Values = businesses.Select(b => b.Name ?? "N/A").ToList(),
                IsDifferent = true // Her zaman farklı olacak
            });

            // Kategori
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Kategori",
                Values = businesses.Select(b => b.Category?.Name ?? "N/A").ToList(),
                IsDifferent = businesses.Select(b => b.CategoryId).Distinct().Count() > 1
            });

            // Fiyat Aralığı
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Fiyat Aralığı",
                Values = businesses.Select(b => b.PriceRange ?? "Belirtilmemiş").ToList(),
                IsDifferent = businesses.Select(b => b.PriceRange ?? "").Distinct().Count() > 1
            });

            // Ortalama Puan
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Ortalama Puan",
                Values = businesses.Select(b => b.AverageRating.ToString("F1")).ToList(),
                IsDifferent = businesses.Select(b => Math.Round(b.AverageRating, 1)).Distinct().Count() > 1
            });

            // Toplam Yorum Sayısı
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Yorum Sayısı",
                Values = businesses.Select(b => b.TotalReviews.ToString()).ToList(),
                IsDifferent = businesses.Select(b => b.TotalReviews).Distinct().Count() > 1
            });

            // Adres
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Adres",
                Values = businesses.Select(b => b.Address ?? "Belirtilmemiş").ToList(),
                IsDifferent = true
            });

            // Telefon
            criticalRows.Add(new ComparisonFeatureRow
            {
                AttributeName = "Telefon",
                Values = businesses.Select(b => b.Phone ?? "Belirtilmemiş").ToList(),
                IsDifferent = true
            });

            return criticalRows;
        }

        /// <summary>
        /// FeaturesJson string'ini List<BusinessFeature>'a dönüştürür
        /// </summary>
        private List<BusinessFeature>? DeserializeFeatures(string? featuresJson)
        {
            if (string.IsNullOrWhiteSpace(featuresJson))
                return new List<BusinessFeature>();

            try
            {
                var features = JsonSerializer.Deserialize<List<BusinessFeature>>(featuresJson);
                return features ?? new List<BusinessFeature>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "FeaturesJson deserialize edilemedi: {Json}", featuresJson);
                return new List<BusinessFeature>();
            }
        }
    }
}

