using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Tests.Helpers;
using Xunit;

namespace Plafind.Tests.Data
{
    public class DbSeederTests : IDisposable
    {
        private readonly ApplicationDbContext _context;

        public DbSeederTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        [Fact]
        public async Task SeedDataAsync_ShouldCreateCategories()
        {
            // Act
            await DbSeeder.SeedDataAsync(_context);

            // Assert
            var categories = await _context.Categories.ToListAsync();
            Assert.NotEmpty(categories);
            Assert.Contains(categories, c => c.Name == "Restoran");
            Assert.Contains(categories, c => c.Name == "Otel");
        }

        [Fact]
        public async Task SeedDataAsync_ShouldCreateBusinesses()
        {
            // Act
            await DbSeeder.SeedDataAsync(_context);

            // Assert
            var businesses = await _context.Businesses.ToListAsync();
            Assert.NotEmpty(businesses);
        }

        [Fact]
        public async Task SeedDataAsync_ShouldNotDuplicateCategories_WhenCalledTwice()
        {
            // Act
            await DbSeeder.SeedDataAsync(_context);
            var firstCount = await _context.Categories.CountAsync();
            
            await DbSeeder.SeedDataAsync(_context);
            var secondCount = await _context.Categories.CountAsync();

            // Assert
            Assert.Equal(firstCount, secondCount);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

