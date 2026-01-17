using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Plafind.Data;
using Plafind.Tests.Helpers;
using System.Net;
using Xunit;

namespace Plafind.Tests.Controllers
{
    public class HomeControllerTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly ApplicationDbContext _context;

        public HomeControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _context = TestDbContextFactory.CreateInMemoryContext();
        }

        [Fact]
        public async Task Index_ShouldReturnSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task About_ShouldReturnSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Home/About");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        public void Dispose()
        {
            _context?.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}

