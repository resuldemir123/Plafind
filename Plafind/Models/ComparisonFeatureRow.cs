namespace Plafind.Models
{
    /// <summary>
    /// Karşılaştırma tablosunda bir özellik satırını temsil eder
    /// </summary>
    public class ComparisonFeatureRow
    {
        /// <summary>
        /// Özellik adı (örn: "WiFi", "Otopark")
        /// </summary>
        public string AttributeName { get; set; } = string.Empty;

        /// <summary>
        /// Her işletme için bu özelliğin değerleri (sıralı)
        /// </summary>
        public List<string> Values { get; set; } = new List<string>();

        /// <summary>
        /// Bu satırdaki değerlerin birbirinden farklı olup olmadığı
        /// Sunucu tarafında hesaplanır (güvenlik için)
        /// </summary>
        public bool IsDifferent { get; set; }
    }
}

