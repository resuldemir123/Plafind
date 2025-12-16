using System.Collections.Generic;

namespace Plafind.Models
{
    /// <summary>
    /// Karşılaştırma sayfası için ViewModel
    /// </summary>
    public class ComparisonViewModel
    {
        /// <summary>
        /// Karşılaştırılan işletmeler
        /// </summary>
        public List<Business> Businesses { get; set; } = new List<Business>();

        /// <summary>
        /// Karşılaştırma özellik satırları
        /// </summary>
        public List<ComparisonFeatureRow> FeatureRows { get; set; } = new List<ComparisonFeatureRow>();

        /// <summary>
        /// Kritik özellikler (isim, fiyat, rating) - her zaman en üstte gösterilir
        /// </summary>
        public List<ComparisonFeatureRow> CriticalFeatures { get; set; } = new List<ComparisonFeatureRow>();
    }
}

