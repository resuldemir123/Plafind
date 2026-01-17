using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Models;

namespace Plafind.Tests.Helpers
{
    /// <summary>
    /// Test için InMemory veritabanı oluşturur
    /// </summary>
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryContext(string? databaseName = null)
        {
            databaseName ??= Guid.NewGuid().ToString();
            
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            
            return context;
        }

        public static async Task<ApplicationDbContext> CreateWithSeedDataAsync()
        {
            var context = CreateInMemoryContext();
            
            // Test kategorileri ekle
            var categories = new List<Category>
            {
                new Category { Name = "Restoran", Description = "Test Restoran Kategorisi" },
                new Category { Name = "Otel", Description = "Test Otel Kategorisi" },
                new Category { Name = "Kafe", Description = "Test Kafe Kategorisi" }
            };
            
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
            
            // Test işletmeleri ekle
            var restaurantCategory = categories.First(c => c.Name == "Restoran");
            var businesses = new List<Business>
            {
                new Business
                {
                    Name = "Test Restoran",
                    Address = "Test Adres",
                    Phone = "+90 555 123 4567",
                    CategoryId = restaurantCategory.Id,
                    Description = "Test açıklama",
                    IsActive = true,
                    IsApproved = true,
                    AverageRating = 4.5,
                    TotalReviews = 10
                }
            };
            
            await context.Businesses.AddRangeAsync(businesses);
            await context.SaveChangesAsync();
            
            return context;
        }
    }
}

