using System;
using System.Collections.Generic;
using System.Linq;
using Plafind.Models;

namespace Plafind.Services
{
    /// <summary>
    /// İşletme özelliklerinin merkezi sözlüğü ve normalleştirme servisi
    /// </summary>
    public class BusinessFeatureDictionary
    {
        /// <summary>
        /// Önceden tanımlı özellik listesi ve görünen adları
        /// </summary>
        private static readonly Dictionary<string, string> _predefinedFeatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Temel Özellikler
            { "WiFi", "WiFi" },
            { "Wifi", "WiFi" },
            { "WLAN", "WiFi" },
            { "Internet", "WiFi" },
            
            // Otopark
            { "Otopark", "Otopark" },
            { "Parking", "Otopark" },
            { "Park Yeri", "Otopark" },
            { "Ücretsiz Otopark", "Otopark" },
            
            // Ödeme
            { "Kredi Kartı", "Kredi Kartı" },
            { "Credit Card", "Kredi Kartı" },
            { "Banka Kartı", "Kredi Kartı" },
            { "Nakit", "Nakit" },
            { "Cash", "Nakit" },
            
            // Mekan Özellikleri
            { "Açık Hava", "Açık Hava" },
            { "Outdoor", "Açık Hava" },
            { "Terras", "Açık Hava" },
            { "Bahçe", "Açık Hava" },
            { "Deniz Manzarası", "Deniz Manzarası" },
            { "Sea View", "Deniz Manzarası" },
            { "Klima", "Klima" },
            { "Air Conditioning", "Klima" },
            { "AC", "Klima" },
            
            // Erişilebilirlik
            { "Tekerlekli Sandalye Erişimi", "Tekerlekli Sandalye Erişimi" },
            { "Wheelchair Access", "Tekerlekli Sandalye Erişimi" },
            { "Engelli Erişimi", "Tekerlekli Sandalye Erişimi" },
            
            // Hizmetler
            { "Kahvaltı", "Kahvaltı" },
            { "Breakfast", "Kahvaltı" },
            { "Room Service", "Oda Servisi" },
            { "Oda Servisi", "Oda Servisi" },
            { "Spa", "Spa" },
            { "Fitness", "Fitness" },
            { "Gym", "Fitness" },
            { "Havuz", "Havuz" },
            { "Pool", "Havuz" },
            { "Plaj", "Plaj" },
            { "Beach", "Plaj" },
            
            // Diğer
            { "Sigara İçilebilir", "Sigara İçilebilir" },
            { "Smoking", "Sigara İçilebilir" },
            { "Sigara İçilemez", "Sigara İçilemez" },
            { "Non-Smoking", "Sigara İçilemez" },
            { "Hayvan Dostu", "Hayvan Dostu" },
            { "Pet Friendly", "Hayvan Dostu" },
            { "Çocuk Dostu", "Çocuk Dostu" },
            { "Kid Friendly", "Çocuk Dostu" },
            { "Açık 24 Saat", "Açık 24 Saat" },
            { "24/7", "Açık 24 Saat" },
            { "Rezervasyon", "Rezervasyon" },
            { "Reservation", "Rezervasyon" },
            { "Teslimat", "Teslimat" },
            { "Delivery", "Teslimat" },
            { "Paket Servis", "Paket Servis" },
            { "Takeaway", "Paket Servis" }
        };

        /// <summary>
        /// Özellik sıralama önceliği (önem sırasına göre)
        /// </summary>
        private static readonly List<string> _featurePriority = new List<string>
        {
            "WiFi",
            "Otopark",
            "Kredi Kartı",
            "Nakit",
            "Açık Hava",
            "Deniz Manzarası",
            "Klima",
            "Tekerlekli Sandalye Erişimi",
            "Kahvaltı",
            "Oda Servisi",
            "Spa",
            "Fitness",
            "Havuz",
            "Plaj",
            "Sigara İçilebilir",
            "Sigara İçilemez",
            "Hayvan Dostu",
            "Çocuk Dostu",
            "Açık 24 Saat",
            "Rezervasyon",
            "Teslimat",
            "Paket Servis"
        };

        /// <summary>
        /// Özellik adını normalleştirir (sözlükteki standart adı döndürür)
        /// </summary>
        public static string NormalizeFeatureName(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
                return featureName;

            // Önce tam eşleşme kontrolü
            if (_predefinedFeatures.TryGetValue(featureName.Trim(), out var normalized))
                return normalized;

            // Büyük/küçük harf duyarsız ve boşluk temizleme ile kontrol
            var cleaned = featureName.Trim();
            var key = _predefinedFeatures.Keys.FirstOrDefault(k => 
                k.Equals(cleaned, StringComparison.OrdinalIgnoreCase) ||
                k.Replace(" ", "").Equals(cleaned.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));

            if (key != null && _predefinedFeatures.TryGetValue(key, out normalized))
                return normalized;

            // Sözlükte yoksa, temizlenmiş halini döndür
            return cleaned;
        }

        /// <summary>
        /// Tüm önceden tanımlı özellik adlarını döndürür
        /// </summary>
        public static List<string> GetPredefinedFeatureNames()
        {
            return _predefinedFeatures.Values.Distinct().ToList();
        }

        /// <summary>
        /// Özellikleri öncelik sırasına göre sıralar
        /// </summary>
        public static List<string> SortFeaturesByPriority(IEnumerable<string> featureNames)
        {
            var sorted = new List<string>();
            var remaining = new List<string>(featureNames);

            // Önce öncelikli özellikleri ekle
            foreach (var priorityFeature in _featurePriority)
            {
                if (remaining.Contains(priorityFeature, StringComparer.OrdinalIgnoreCase))
                {
                    sorted.Add(priorityFeature);
                    remaining.RemoveAll(f => f.Equals(priorityFeature, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Kalan özellikleri alfabetik olarak ekle
            sorted.AddRange(remaining.OrderBy(f => f));

            return sorted;
        }

        /// <summary>
        /// İşletme özelliklerini normalleştirir
        /// </summary>
        public static List<BusinessFeature> NormalizeBusinessFeatures(List<BusinessFeature>? features)
        {
            if (features == null || !features.Any())
                return new List<BusinessFeature>();

            var normalized = features
                .Select(f => new BusinessFeature
                {
                    Name = NormalizeFeatureName(f.Name),
                    Value = f.Value?.Trim() ?? string.Empty,
                    DisplayName = f.DisplayName ?? NormalizeFeatureName(f.Name)
                })
                .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First()) // Duplicate'leri kaldır
                .ToList();

            return normalized;
        }

        /// <summary>
        /// Özellik adının sözlükte olup olmadığını kontrol eder
        /// </summary>
        public static bool IsPredefinedFeature(string featureName)
        {
            if (string.IsNullOrWhiteSpace(featureName))
                return false;

            return _predefinedFeatures.ContainsKey(featureName.Trim()) ||
                   _predefinedFeatures.Keys.Any(k => 
                       k.Equals(featureName.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}

