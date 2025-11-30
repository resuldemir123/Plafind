namespace Plafind.Models
{
    public class AdminLog
    {
        public int Id { get; set; }
        public string? AdminUserId { get; set; }
        public string? Action { get; set; } // "Create", "Update", "Delete", "Approve", etc.
        public string? EntityType { get; set; } // "Business", "User", "Review", etc.
        public string? EntityId { get; set; } // int? yerine string? olarak g�ncellendi
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}