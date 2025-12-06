using System;
using System.Threading.Tasks;
using Plafind.Models;
using Microsoft.AspNetCore.Identity;

namespace Plafind.Data
{
    public static class IdentitySeeder
    {
        private const string AdminEmail = "admin@plafind.com";
        private const string AdminPassword = "Admin123!";
        private const string AdminRoleName = "Admin";

        private const string BusinessOwnerRoleName = "BusinessOwner";
        private const string UserRoleName = "User";

        public static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            if (userManager == null) throw new ArgumentNullException(nameof(userManager));
            if (roleManager == null) throw new ArgumentNullException(nameof(roleManager));

            // Roller: Admin, BusinessOwner, User
            if (!await roleManager.RoleExistsAsync(AdminRoleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(AdminRoleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("Admin rolü oluşturulamadı: " + string.Join(", ", roleResult.Errors));
                }
            }

            if (!await roleManager.RoleExistsAsync(BusinessOwnerRoleName))
            {
                var boRoleResult = await roleManager.CreateAsync(new IdentityRole(BusinessOwnerRoleName));
                if (!boRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("BusinessOwner rolü oluşturulamadı: " + string.Join(", ", boRoleResult.Errors));
                }
            }

            if (!await roleManager.RoleExistsAsync(UserRoleName))
            {
                var userRoleResult = await roleManager.CreateAsync(new IdentityRole(UserRoleName));
                if (!userRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("User rolü oluşturulamadı: " + string.Join(", ", userRoleResult.Errors));
                }
            }

            var adminUser = await userManager.FindByEmailAsync(AdminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = AdminEmail,
                    Email = AdminEmail,
                    EmailConfirmed = true,
                    FullName = "Sistem Yöneticisi",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var userResult = await userManager.CreateAsync(adminUser, AdminPassword);
                if (!userResult.Succeeded)
                {
                    throw new InvalidOperationException("Admin kullanıcısı oluşturulamadı: " + string.Join(", ", userResult.Errors));
                }
            }

            if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
            {
                var addRoleResult = await userManager.AddToRoleAsync(adminUser, AdminRoleName);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("Admin rolü atanamadı: " + string.Join(", ", addRoleResult.Errors));
                }
            }
        }
    }
}

