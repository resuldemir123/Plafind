using Plafind.Models;
using Plafind.Tests.Helpers;
using Xunit;

namespace Plafind.Tests.Models
{
    public class BusinessTests
    {
        [Fact]
        public void Business_ShouldHaveRequiredProperties()
        {
            // Arrange & Act
            var business = TestDataBuilder.CreateTestBusiness(1, "Test İşletme");

            // Assert
            Assert.NotNull(business.Name);
            Assert.NotNull(business.Address);
            Assert.True(business.IsActive);
        }

        [Fact]
        public void Business_ViewCount_ShouldDefaultToZero()
        {
            // Arrange & Act
            var business = new Business();

            // Assert
            Assert.Equal(0, business.ViewCount);
        }

        [Fact]
        public void Business_AverageRating_ShouldBeBetweenZeroAndFive()
        {
            // Arrange & Act
            var business = TestDataBuilder.CreateTestBusiness(1);
            business.AverageRating = 4.5;

            // Assert
            Assert.True(business.AverageRating >= 0 && business.AverageRating <= 5);
        }
    }
}

