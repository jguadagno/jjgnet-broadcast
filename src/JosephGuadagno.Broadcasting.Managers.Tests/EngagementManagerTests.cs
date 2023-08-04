using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EngagementManagerTests
{
    [Fact]
    public void UpdateDateTimeOffsetWithTimeZoneTest()
    {
        // Arrange
        var repository = new Mock<Domain.Interfaces.IEngagementRepository>();
        var engagementManager = new EngagementManager(repository.Object);

        // Should be AZ
        var localDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0));
        // Should be current time 'America/New_York
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset =
            engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", localDateTime);

        // Assert
        Assert.Equal(expectedDateTimeOffset, actualDateTimeOffset);
    }
    
    [Fact]
    public void UpdateDateTimeOffsetWithTimeZoneTest2()
    {
        // Arrange
        var repository = new Mock<Domain.Interfaces.IEngagementRepository>();
        var engagementManager = new EngagementManager(repository.Object);

        // Should be AZ
        var localDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(+5, 0, 0));
        // Should be current time 'America/New_York
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset =
            engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", localDateTime);

        // Assert
        Assert.Equal(expectedDateTimeOffset, actualDateTimeOffset);
    }
}