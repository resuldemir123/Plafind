using Microsoft.EntityFrameworkCore;
using Plafind.Models;

namespace Plafind.Data
{
    /// <summary>
    /// Permission seed data - İlk izinleri veritabanına ekler
    /// </summary>
    public static class PermissionSeeder
    {
        public static async Task SeedPermissionsAsync(ApplicationDbContext context)
        {
            // Tüm izinleri tanımla
            var permissions = new[]
            {
                // Businesses
                new Permission { Name = Permissions.Businesses_View, DisplayName = "İşletme Görüntüle", Category = "Businesses", Description = "İşletmeleri görüntüleme izni" },
                new Permission { Name = Permissions.Businesses_Create, DisplayName = "İşletme Oluştur", Category = "Businesses", Description = "Yeni işletme oluşturma izni" },
                new Permission { Name = Permissions.Businesses_Edit, DisplayName = "İşletme Düzenle", Category = "Businesses", Description = "İşletme bilgilerini düzenleme izni" },
                new Permission { Name = Permissions.Businesses_Delete, DisplayName = "İşletme Sil", Category = "Businesses", Description = "İşletme silme izni" },
                new Permission { Name = Permissions.Businesses_Approve, DisplayName = "İşletme Onayla", Category = "Businesses", Description = "İşletme onaylama izni" },
                new Permission { Name = Permissions.Businesses_BulkAction, DisplayName = "Toplu İşlem", Category = "Businesses", Description = "Toplu işlem yapma izni" },
                
                // Users
                new Permission { Name = Permissions.Users_View, DisplayName = "Kullanıcı Görüntüle", Category = "Users", Description = "Kullanıcıları görüntüleme izni" },
                new Permission { Name = Permissions.Users_Edit, DisplayName = "Kullanıcı Düzenle", Category = "Users", Description = "Kullanıcı bilgilerini düzenleme izni" },
                new Permission { Name = Permissions.Users_Ban, DisplayName = "Kullanıcı Banla", Category = "Users", Description = "Kullanıcı banlama izni" },
                new Permission { Name = Permissions.Users_Delete, DisplayName = "Kullanıcı Sil", Category = "Users", Description = "Kullanıcı silme izni" },
                
                // Reviews
                new Permission { Name = Permissions.Reviews_View, DisplayName = "Yorum Görüntüle", Category = "Reviews", Description = "Yorumları görüntüleme izni" },
                new Permission { Name = Permissions.Reviews_Moderate, DisplayName = "Yorum Moderasyon", Category = "Reviews", Description = "Yorum moderasyon izni" },
                new Permission { Name = Permissions.Reviews_Delete, DisplayName = "Yorum Sil", Category = "Reviews", Description = "Yorum silme izni" },
                
                // Admin
                new Permission { Name = Permissions.Admin_Dashboard, DisplayName = "Admin Dashboard", Category = "Admin", Description = "Admin dashboard görüntüleme izni" },
                new Permission { Name = Permissions.Admin_Logs, DisplayName = "Admin Logları", Category = "Admin", Description = "Admin loglarını görüntüleme izni" },
                new Permission { Name = Permissions.Admin_Settings, DisplayName = "Admin Ayarları", Category = "Admin", Description = "Admin ayarlarını düzenleme izni" }
            };

            foreach (var permission in permissions)
            {
                var existing = await context.Permissions
                    .FirstOrDefaultAsync(p => p.Name == permission.Name);

                if (existing == null)
                {
                    context.Permissions.Add(permission);
                }
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Admin rolüne tüm izinleri atar
        /// </summary>
        public static async Task AssignPermissionsToAdminRoleAsync(ApplicationDbContext context, string adminRoleId)
        {
            if (string.IsNullOrEmpty(adminRoleId))
                return;

            var allPermissions = await context.Permissions.ToListAsync();
            var existingRolePermissions = await context.RolePermissions
                .Where(rp => rp.RoleId == adminRoleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            foreach (var permission in allPermissions)
            {
                if (!existingRolePermissions.Contains(permission.Id))
                {
                    context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRoleId,
                        PermissionId = permission.Id
                    });
                }
            }

            await context.SaveChangesAsync();
        }
    }
}

