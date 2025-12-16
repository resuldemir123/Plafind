namespace Plafind.Models
{
    /// <summary>
    /// İşletme dinamik özelliklerini temsil eden model
    /// JSON formatında Business.FeaturesJson içinde saklanır
    /// </summary>
    public class BusinessFeature
    {
        /// <summary>
        /// Özellik adı (örn: "WiFi", "Otopark", "Kredi Kartı")
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Özellik değeri (örn: "Var", "Yok", "100 Mbps", "Ücretsiz")
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Görünen ad (i18n için, opsiyonel)
        /// </summary>
        public string? DisplayName { get; set; }
    }
}

