namespace Plafind.Models
{
    /// <summary>
    /// İzin sistemi - Rol bazlı yetkilendirme
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty; // "Businesses.Edit", "Users.Ban", etc.
        public string DisplayName { get; set; } = string.Empty; // "İşletme Düzenle"
        public string Category { get; set; } = string.Empty; // "Businesses", "Users", "Reviews", etc.
        public string? Description { get; set; }
    }
    
    /// <summary>
    /// Rol-İzin ilişkisi
    /// </summary>
    public class RolePermission
    {
        public int Id { get; set; }
        public string RoleId { get; set; } = string.Empty;
        public int PermissionId { get; set; }
        
        // Navigation properties
        public Microsoft.AspNetCore.Identity.IdentityRole? Role { get; set; }
        public Permission? Permission { get; set; }
    }
    
    /// <summary>
    /// Önceden tanımlı izinler
    /// </summary>
    public static class Permissions
    {
        // Businesses
        public const string Businesses_View = "Businesses.View";
        public const string Businesses_Create = "Businesses.Create";
        public const string Businesses_Edit = "Businesses.Edit";
        public const string Businesses_Delete = "Businesses.Delete";
        public const string Businesses_Approve = "Businesses.Approve";
        public const string Businesses_BulkAction = "Businesses.BulkAction";
        
        // Users
        public const string Users_View = "Users.View";
        public const string Users_Edit = "Users.Edit";
        public const string Users_Ban = "Users.Ban";
        public const string Users_Delete = "Users.Delete";
        
        // Reviews
        public const string Reviews_View = "Reviews.View";
        public const string Reviews_Moderate = "Reviews.Moderate";
        public const string Reviews_Delete = "Reviews.Delete";
        
        // Admin
        public const string Admin_Dashboard = "Admin.Dashboard";
        public const string Admin_Logs = "Admin.Logs";
        public const string Admin_Settings = "Admin.Settings";
    }
}

