using Plafind.Models;

namespace Plafind.Tests.Helpers
{
    /// <summary>
    /// Test verileri oluşturmak için builder pattern
    /// </summary>
    public class TestDataBuilder
    {
        public static Business CreateTestBusiness(int categoryId, string? name = null)
        {
            return new Business
            {
                Name = name ?? "Test İşletme",
                Address = "Test Adres",
                Phone = "+90 555 123 4567",
                CategoryId = categoryId,
                Description = "Test açıklama",
                Email = "test@example.com",
                Website = "www.test.com",
                WorkingHours = "09:00-18:00",
                PriceRange = "₺₺",
                IsActive = true,
                IsApproved = true,
                AverageRating = 4.0,
                TotalReviews = 0,
                ViewCount = 0
            };
        }

        public static Review CreateTestReview(int businessId, string userId, int rating = 5, string? comment = null)
        {
            return new Review
            {
                BusinessId = businessId,
                UserId = userId,
                Rating = rating,
                Comment = comment ?? "Test yorum",
                CreatedDate = DateTime.Now,
                IsApproved = true,
                IsActive = true
            };
        }

        public static Category CreateTestCategory(string? name = null)
        {
            return new Category
            {
                Name = name ?? "Test Kategori",
                Description = "Test kategori açıklaması"
            };
        }
    }
}

