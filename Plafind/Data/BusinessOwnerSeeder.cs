using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Plafind.Models;

namespace Plafind.Data
{
    public static class BusinessOwnerSeeder
    {
        private const string BusinessOwnerEmail = "owner@plafind.com";
        private const string BusinessOwnerPassword = "Owner123!";
        private const string BusinessOwnerRoleName = "BusinessOwner";

        public static async Task SeedBusinessOwnerAsync(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (userManager == null) throw new ArgumentNullException(nameof(userManager));
            if (roleManager == null) throw new ArgumentNullException(nameof(roleManager));

            // BusinessOwner rolü yoksa oluştur
            if (!await roleManager.RoleExistsAsync(BusinessOwnerRoleName))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(BusinessOwnerRoleName));
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException("BusinessOwner rolü oluşturulamadı: " +
                                                        string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            // Örnek işletme sahibi kullanıcı
            var ownerUser = await userManager.FindByEmailAsync(BusinessOwnerEmail);
            if (ownerUser == null)
            {
                ownerUser = new ApplicationUser
                {
                    UserName = BusinessOwnerEmail,
                    Email = BusinessOwnerEmail,
                    EmailConfirmed = true,
                    FullName = "Örnek İşletme Sahibi",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var userResult = await userManager.CreateAsync(ownerUser, BusinessOwnerPassword);
                if (!userResult.Succeeded)
                {
                    throw new InvalidOperationException("BusinessOwner kullanıcısı oluşturulamadı: " +
                                                        string.Join(", ", userResult.Errors.Select(e => e.Description)));
                }
            }

            // Role ataması
            if (!await userManager.IsInRoleAsync(ownerUser, BusinessOwnerRoleName))
            {
                var addRoleResult = await userManager.AddToRoleAsync(ownerUser, BusinessOwnerRoleName);
                if (!addRoleResult.Succeeded)
                {
                    throw new InvalidOperationException("BusinessOwner rolü atanamadı: " +
                                                        string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }

            // OwnerId atanmamış işletmeleri bu kullanıcıya bağla (örnek/demo amaçlı)
            var businessesWithoutOwner = await context.Businesses
                .Where(b => b.OwnerId == null)
                .ToListAsync();

            if (businessesWithoutOwner.Any())
            {
                foreach (var business in businessesWithoutOwner)
                {
                    business.OwnerId = ownerUser.Id;
                }

                await context.SaveChangesAsync();
            }
        }
    }
}


