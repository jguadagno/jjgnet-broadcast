using Moq;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EngagementManagerTests
{
    private readonly Mock<IEngagementRepository> _repository;
    private readonly EngagementManager _engagementManager;

    public EngagementManagerTests()
    {
        _repository = new Mock<IEngagementRepository>();
        _engagementManager = new EngagementManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(engagement);

        // Act
        var result = await _engagementManager.GetAsync(1);

        // Assert
        Assert.Equal(engagement, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNewEngagement_ShouldFindExistingEngagement()
    {
        // Arrange
        var engagement = new Engagement { Id = 0, Name = "Test", Url = "http://test.com", StartDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, TimeSpan.Zero), TimeZoneId = "UTC" };
        var existingEngagement = new Engagement { Id = 5 };
        _repository.Setup(r => r.GetByNameAndUrlAndYearAsync("Test", "http://test.com", 2022)).ReturnsAsync(existingEngagement);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement e) => e);

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.Equal(5, result.Id);
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Test", "http://test.com", 2022), Times.Once);
        _repository.Verify(r => r.SaveAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNewEngagement_ShouldNotChangeIdIfNotFound()
    {
        // Arrange
        var engagement = new Engagement { Id = 0, Name = "Test", Url = "http://test.com", StartDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, TimeSpan.Zero), TimeZoneId = "UTC" };
        _repository.Setup(r => r.GetByNameAndUrlAndYearAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ReturnsAsync((Engagement)null);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement e) => e);

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.Equal(0, result.Id);
        _repository.Verify(r => r.SaveAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldUpdateTimeZone()
    {
        // Arrange
        var engagement = new Engagement 
        { 
            Id = 1, 
            TimeZoneId = "America/New_York", 
            StartDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0)),
            EndDateTime = new DateTimeOffset(2022, 1, 1, 13, 0, 0, new TimeSpan(-7, 0, 0))
        };
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement e) => e);

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.Equal(new TimeSpan(-5, 0, 0), result.StartDateTime.Offset);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.EndDateTime.Offset);
        _repository.Verify(r => r.SaveAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var engagements = new List<Engagement> { new Engagement { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(engagements);

        // Act
        var result = await _engagementManager.GetAllAsync();

        // Assert
        Assert.Equal(engagements, result);
        _repository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(engagement)).ReturnsAsync(true);

        // Act
        var result = await _engagementManager.DeleteAsync(engagement);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(engagement), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _engagementManager.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetTalksForEngagementAsync_ShouldCallRepository()
    {
        // Arrange
        var talks = new List<Talk> { new Talk { Id = 1 } };
        _repository.Setup(r => r.GetTalksForEngagementAsync(1)).ReturnsAsync(talks);

        // Act
        var result = await _engagementManager.GetTalksForEngagementAsync(1);

        // Assert
        Assert.Equal(talks, result);
        _repository.Verify(r => r.GetTalksForEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveTalkAsync_ShouldThrowIfEngagementNotFound()
    {
        // Arrange
        var talk = new Talk { EngagementId = 1 };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync((Engagement)null);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _engagementManager.SaveTalkAsync(talk));
    }

    [Fact]
    public async Task SaveTalkAsync_ShouldUpdateTimeZoneAndSave()
    {
        // Arrange
        var engagement = new Engagement { Id = 1, TimeZoneId = "America/New_York" };
        var talk = new Talk 
        { 
            EngagementId = 1, 
            StartDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0)),
            EndDateTime = new DateTimeOffset(2022, 1, 1, 13, 0, 0, new TimeSpan(-7, 0, 0))
        };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(engagement);
        _repository.Setup(r => r.SaveTalkAsync(It.IsAny<Talk>())).ReturnsAsync((Talk t) => t);

        // Act
        var result = await _engagementManager.SaveTalkAsync(talk);

        // Assert
        Assert.Equal(new TimeSpan(-5, 0, 0), result.StartDateTime.Offset);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.EndDateTime.Offset);
        _repository.Verify(r => r.SaveTalkAsync(talk), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_Id_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.RemoveTalkFromEngagementAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _engagementManager.RemoveTalkFromEngagementAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.RemoveTalkFromEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_Talk_ShouldCallRepository()
    {
        // Arrange
        var talk = new Talk { Id = 1 };
        _repository.Setup(r => r.RemoveTalkFromEngagementAsync(talk)).ReturnsAsync(true);

        // Act
        var result = await _engagementManager.RemoveTalkFromEngagementAsync(talk);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.RemoveTalkFromEngagementAsync(talk), Times.Once);
    }

    [Fact]
    public async Task GetTalkAsync_ShouldCallRepository()
    {
        // Arrange
        var talk = new Talk { Id = 1 };
        _repository.Setup(r => r.GetTalkAsync(1)).ReturnsAsync(talk);

        // Act
        var result = await _engagementManager.GetTalkAsync(1);

        // Assert
        Assert.Equal(talk, result);
        _repository.Verify(r => r.GetTalkAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_ShouldCallRepository()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        _repository.Setup(r => r.GetByNameAndUrlAndYearAsync("Name", "Url", 2022)).ReturnsAsync(engagement);

        // Act
        var result = await _engagementManager.GetByNameAndUrlAndYearAsync("Name", "Url", 2022);

        // Assert
        Assert.Equal(engagement, result);
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Name", "Url", 2022), Times.Once);
    }

    [Fact]
    public void UpdateDateTimeOffsetWithTimeZoneTest()
    {
        // Arrange
        // Should be AZ
        var localDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0));
        // Should be current time 'America/New_York
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset =
            _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", localDateTime);

        // Assert
        Assert.Equal(expectedDateTimeOffset, actualDateTimeOffset);
    }
    
    [Fact]
    public void UpdateDateTimeOffsetWithTimeZoneTest2()
    {
        // Arrange
        // Should be AZ
        var localDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(+5, 0, 0));
        // Should be current time 'America/New_York
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset =
            _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", localDateTime);

        // Assert
        Assert.Equal(expectedDateTimeOffset, actualDateTimeOffset);
    }
}