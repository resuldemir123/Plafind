using Microsoft.EntityFrameworkCore;
using Moq;
using Plafind.Data;
using Plafind.Features.Businesses.Services;
using Plafind.Models;
using Plafind.Tests.Helpers;
using Xunit;

namespace Plafind.Tests.Services
{
    public class BusinessServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly BusinessService _service;
        private readonly Mock<ILocationService> _locationServiceMock;

        public BusinessServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _locationServiceMock = new Mock<ILocationService>();
            _service = new BusinessService(_context, _locationServiceMock.Object);
        }

        [Fact]
        public async Task GetBusinessDetailsAsync_ShouldReturnBusiness_WhenExists()
        {
            // Arrange
            var category = new Category { Name = "Test", Description = "Test" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            var business = TestDataBuilder.CreateTestBusiness(category.Id, "Test İşletme");
            await _context.Businesses.AddAsync(business);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetBusinessDetailsAsync(business.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Business);
            Assert.Equal("Test İşletme", result.Business.Name);
        }

        [Fact]
        public async Task GetBusinessDetailsAsync_ShouldReturnNull_WhenNotExists()
        {
            // Act
            var result = await _service.GetBusinessDetailsAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetBusinessDetailsAsync_ShouldIncrementViewCount()
        {
            // Arrange
            var category = new Category { Name = "Test", Description = "Test" };
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();

            var business = TestDataBuilder.CreateTestBusiness(category.Id);
            business.ViewCount = 5;
            await _context.Businesses.AddAsync(business);
            await _context.SaveChangesAsync();

            // Act
            await _service.GetBusinessDetailsAsync(business.Id);

            // Assert
            var updated = await _context.Businesses.FindAsync(business.Id);
            Assert.Equal(6, updated?.ViewCount);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}

