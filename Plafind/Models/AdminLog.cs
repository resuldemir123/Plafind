namespace Plafind.Models
{
    /// <summary>
    /// Admin işlem logları (Audit Log)
    /// </summary>
    public class AdminLog
    {
        public int Id { get; set; }
        public string? AdminUserId { get; set; }
        public string? AdminUserName { get; set; } // Admin kullanıcı adı (cache)
        public string? Action { get; set; } // "Create", "Update", "Delete", "Approve", "Restore", etc.
        public string? EntityType { get; set; } // "Business", "User", "Review", etc.
        public string? EntityId { get; set; } // Entity ID
        public string? EntityName { get; set; } // Entity adı (örn: İşletme adı)
        public string? Description { get; set; }
        public string? OldValues { get; set; } // JSON formatında eski değerler
        public string? NewValues { get; set; } // JSON formatında yeni değerler
        public string? IpAddress { get; set; } // IP adresi
        public string? UserAgent { get; set; } // User agent
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        // Navigation property
        public ApplicationUser? AdminUser { get; set; }
    }
}