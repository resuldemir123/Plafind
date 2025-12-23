namespace Plafind.Models
{
    /// <summary>
    /// İşletme durumları
    /// </summary>
    public enum BusinessStatus
    {
        Draft = 0,      // Taslak
        Pending = 1,   // Onay bekliyor
        Published = 2, // Yayında
        Archived = 3,  // Arşivlendi
        Rejected = 4   // Reddedildi
    }
}

