using FluentAssertions;
using WebAPI.Domain;

namespace WebAPI.Tests.UnitTests;

public class PageLimitsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_Number_And_Size_Below_One_Become_One(int value)
    {
        var query = new ProductQueryParameters
        {
            PageNumber = value,
            PageSize = value
        };

        query.PageNumber.Should().Be(1);
        query.PageSize.Should().Be(1);
        PageLimits.PageNumber(value).Should().Be(1);
        PageLimits.PageSize(value).Should().Be(1);
    }

    [Fact]
    public void Page_Size_Above_50_Becomes_50()
    {
        var query = new ProductQueryParameters { PageSize = 10_000 };

        query.PageSize.Should().Be(50);
        PageLimits.PageSize(10_000).Should().Be(50);
    }
}
