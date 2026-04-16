using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class EngagementManagerTests
{
    private readonly Mock<IEngagementDataStore> _repository;
    private readonly EngagementManager _engagementManager;

    public EngagementManagerTests()
    {
        _repository = new Mock<IEngagementDataStore>();
        var logger = new Mock<ILogger<EngagementManager>>();
        _engagementManager = new EngagementManager(_repository.Object, logger.Object);
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
        _repository.Setup(r => r.GetByNameAndUrlAndYearAsync("Test", "http://test.com", 2022, default)).ReturnsAsync(existingEngagement);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default)).ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Id);
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Test", "http://test.com", 2022, default), Times.Once);
        _repository.Verify(r => r.SaveAsync(engagement, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithNewEngagement_ShouldNotChangeIdIfNotFound()
    {
        // Arrange
        var engagement = new Engagement { Id = 0, Name = "Test", Url = "http://test.com", StartDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, TimeSpan.Zero), TimeZoneId = "UTC" };
        _repository.Setup(r => r.GetByNameAndUrlAndYearAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), default)).ReturnsAsync((Engagement)null);
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default)).ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.Id);
        _repository.Verify(r => r.SaveAsync(engagement, default), Times.Once);
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
        _repository.Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default)).ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.Value!.StartDateTime.Offset);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.Value!.EndDateTime.Offset);
        _repository.Verify(r => r.SaveAsync(engagement, default), Times.Once);
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
        _repository.Setup(r => r.DeleteAsync(engagement, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _engagementManager.DeleteAsync(engagement);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(engagement, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _engagementManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
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
    public async Task SaveTalkAsync_ShouldReturnFailureIfEngagementNotFound()
    {
        // Arrange
        var talk = new Talk { EngagementId = 1 };
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync((Engagement)null);

        // Act
        var result = await _engagementManager.SaveTalkAsync(talk);

        // Assert
        Assert.False(result.IsSuccess);
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
        _repository.Setup(r => r.GetAsync(1, default)).ReturnsAsync(engagement);
        _repository.Setup(r => r.SaveTalkAsync(It.IsAny<Talk>(), default)).ReturnsAsync((Talk t, CancellationToken ct) => OperationResult<Talk>.Success(t));

        // Act
        var result = await _engagementManager.SaveTalkAsync(talk);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.Value!.StartDateTime.Offset);
        Assert.Equal(new TimeSpan(-5, 0, 0), result.Value!.EndDateTime.Offset);
        _repository.Verify(r => r.SaveTalkAsync(talk, default), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_Id_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.RemoveTalkFromEngagementAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _engagementManager.RemoveTalkFromEngagementAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.RemoveTalkFromEngagementAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_Talk_ShouldCallRepository()
    {
        // Arrange
        var talk = new Talk { Id = 1 };
        _repository.Setup(r => r.RemoveTalkFromEngagementAsync(talk, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _engagementManager.RemoveTalkFromEngagementAsync(talk);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.RemoveTalkFromEngagementAsync(talk, default), Times.Once);
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
    public void UpdateDateTimeOffsetWithTimeZone_EasternStandardTimeWithWinterTime_ShouldConvertToUTC()
    {
        // Arrange
        // Input: 12:00 with UTC-7 offset (Arizona time, which doesn't observe DST)
        // Should be interpreted as 12:00 EST (UTC-5 in winter) since we're saying it's in EST timezone
        var inputDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-7, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(-5));
    }
    
    [Fact]
    public void UpdateDateTimeOffsetWithTimeZone_EasternStandardTimeWithPositiveOffset_ShouldConvertToUTC()
    {
        // Arrange
        // Input: 12:00 with UTC+5 offset (e.g., Pakistan time)
        // Should be re-interpreted as 12:00 EST (UTC-5 in winter) since we're saying it's in EST timezone
        var inputDateTime = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(+5, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 12, 0, 0, new TimeSpan(-5, 0, 0));
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/New_York", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    [Fact]
    public void UpdateDateTimeOffsetWithTimeZone_PacificStandardTimeWithWinterTime_ShouldConvertToUTC()
    {
        // Arrange
        // Input: 08:00 on Jan 1 (winter time)
        // Pacific timezone is UTC-8 in winter
        var inputDateTime = new DateTimeOffset(2022, 1, 1, 8, 0, 0, new TimeSpan(-7, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 1, 1, 8, 0, 0, new TimeSpan(-8, 0, 0));
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/Los_Angeles", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(-8));
    }

    [Fact]
    public void UpdateDateTimeOffsetWithTimeZone_PacificDaylightTimeWithSummerTime_ShouldConvertToUTC()
    {
        // Arrange
        // Input: 10:00 on Jul 15 (summer time)
        // Pacific timezone is UTC-7 in summer (daylight saving)
        var inputDateTime = new DateTimeOffset(2022, 7, 15, 10, 0, 0, new TimeSpan(-5, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 7, 15, 10, 0, 0, new TimeSpan(-7, 0, 0));
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("America/Los_Angeles", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(-7));
    }

    [Fact]
    public void UpdateDateTimeOffsetWithTimeZone_UTCTimezone_ShouldRemainUTC()
    {
        // Arrange
        var inputDateTime = new DateTimeOffset(2022, 6, 15, 14, 30, 0, new TimeSpan(0, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 6, 15, 14, 30, 0, TimeSpan.Zero);
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("UTC", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void UpdateDateTimeOffsetWithTimeZone_CentralEuropeanTime_ShouldConvertCorrectly()
    {
        // Arrange
        // Input: 15:00 on Dec 1 (winter time, CET is UTC+1)
        var inputDateTime = new DateTimeOffset(2022, 12, 1, 15, 0, 0, new TimeSpan(-5, 0, 0));
        var expectedDateTimeOffset = new DateTimeOffset(2022, 12, 1, 15, 0, 0, new TimeSpan(1, 0, 0));
        
        // Act
        var actualDateTimeOffset = _engagementManager.UpdateDateTimeOffsetWithTimeZone("Europe/Berlin", inputDateTime);

        // Assert
        actualDateTimeOffset.Should().Be(expectedDateTimeOffset);
        actualDateTimeOffset.Offset.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public async Task SaveAsync_WithDeduplication_ShouldReuseDuplicateEngagementId()
    {
        // Arrange
        // Create a new engagement with Id = 0 (triggers deduplication check)
        var newEngagement = new Engagement 
        { 
            Id = 0,
            Name = "Tech Conference", 
            Url = "https://techconf.example.com",
            StartDateTime = new DateTimeOffset(2023, 6, 15, 9, 0, 0, new TimeSpan(-5, 0, 0)),
            EndDateTime = new DateTimeOffset(2023, 6, 15, 17, 0, 0, new TimeSpan(-5, 0, 0)),
            TimeZoneId = "America/New_York"
        };

        // Simulate an existing engagement in the database with same name, URL, and year
        var existingEngagement = new Engagement 
        { 
            Id = 42,
            Name = "Tech Conference",
            Url = "https://techconf.example.com",
            StartDateTime = new DateTimeOffset(2023, 1, 10, 10, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2023, 1, 10, 18, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };

        _repository
            .Setup(r => r.GetByNameAndUrlAndYearAsync("Tech Conference", "https://techconf.example.com", 2023, default))
            .ReturnsAsync(existingEngagement);
        _repository
            .Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default))
            .ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(newEngagement);

        // Assert
        // Verify that the deduplication logic found the existing engagement
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(42, "new engagement should have been assigned the existing engagement's ID");
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Tech Conference", "https://techconf.example.com", 2023, default), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithoutDeduplication_ShouldNotSearchIfIdIsNonZero()
    {
        // Arrange
        var existingEngagement = new Engagement 
        { 
            Id = 15,
            Name = "Existing Event",
            Url = "https://existing.example.com",
            StartDateTime = new DateTimeOffset(2023, 3, 1, 10, 0, 0, new TimeSpan(-5, 0, 0)),
            EndDateTime = new DateTimeOffset(2023, 3, 1, 18, 0, 0, new TimeSpan(-5, 0, 0)),
            TimeZoneId = "America/New_York"
        };

        _repository
            .Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default))
            .ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(existingEngagement);

        // Assert
        // Verify that GetByNameAndUrlAndYearAsync was NOT called because Id is non-zero
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(15);
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), default), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_WithTimezoneCorrection_ShouldApplyToStartAndEndDateTime()
    {
        // Arrange
        var engagement = new Engagement 
        { 
            Id = 1, 
            Name = "Event",
            Url = "https://event.example.com",
            TimeZoneId = "America/New_York", 
            StartDateTime = new DateTimeOffset(2022, 6, 15, 14, 0, 0, new TimeSpan(-7, 0, 0)),
            EndDateTime = new DateTimeOffset(2022, 6, 15, 16, 0, 0, new TimeSpan(-7, 0, 0))
        };

        _repository
            .Setup(r => r.SaveAsync(It.IsAny<Engagement>(), default))
            .ReturnsAsync((Engagement e, CancellationToken ct) => OperationResult<Engagement>.Success(e));

        // Act
        var result = await _engagementManager.SaveAsync(engagement);

        // Assert
        // June 15, 2022 is during EDT (Eastern Daylight Time, UTC-4)
        result.IsSuccess.Should().BeTrue();
        result.Value!.StartDateTime.Offset.Should().Be(TimeSpan.FromHours(-4));
        result.Value!.EndDateTime.Offset.Should().Be(TimeSpan.FromHours(-4));
        result.Value!.StartDateTime.Hour.Should().Be(14);
        result.Value!.EndDateTime.Hour.Should().Be(16);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_WithValidParameters_ShouldReturnEngagementFromRepository()
    {
        // Arrange
        var expectedEngagement = new Engagement 
        { 
            Id = 7,
            Name = "Code Summit",
            Url = "https://codesummit.example.com",
            StartDateTime = new DateTimeOffset(2024, 9, 20, 9, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2024, 9, 20, 17, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };

        _repository
            .Setup(r => r.GetByNameAndUrlAndYearAsync("Code Summit", "https://codesummit.example.com", 2024))
            .ReturnsAsync(expectedEngagement);

        // Act
        var result = await _engagementManager.GetByNameAndUrlAndYearAsync("Code Summit", "https://codesummit.example.com", 2024);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(7);
        result.Name.Should().Be("Code Summit");
        result.Url.Should().Be("https://codesummit.example.com");
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Code Summit", "https://codesummit.example.com", 2024), Times.Once);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_WithNoDuplicateFound_ShouldReturnNull()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByNameAndUrlAndYearAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((Engagement)null);

        // Act
        var result = await _engagementManager.GetByNameAndUrlAndYearAsync("Nonexistent Event", "https://nonexistent.example.com", 2025);

        // Assert
        result.Should().BeNull();
        _repository.Verify(r => r.GetByNameAndUrlAndYearAsync("Nonexistent Event", "https://nonexistent.example.com", 2025), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithDefaultParameters_CallsDataStoreWithDefaults()
    {
        // Arrange
        var pagedResult = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1 } },
            TotalCount = 1
        };
        _repository
            .Setup(r => r.GetAllAsync(1, 10, "startdate", true, null, default))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _engagementManager.GetAllAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _repository.Verify(r => r.GetAllAsync(1, 10, "startdate", true, null, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithSortByName_CallsDataStoreWithNameSort()
    {
        // Arrange
        var pagedResult = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Alpha" } },
            TotalCount = 1
        };
        _repository
            .Setup(r => r.GetAllAsync(1, 10, "name", false, null, default))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _engagementManager.GetAllAsync(1, 10, "name", false);

        // Assert
        result.Should().NotBeNull();
        _repository.Verify(r => r.GetAllAsync(1, 10, "name", false, null, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithFilter_CallsDataStoreWithFilter()
    {
        // Arrange
        var pagedResult = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Tech Conference" } },
            TotalCount = 1
        };
        _repository
            .Setup(r => r.GetAllAsync(1, 10, "startdate", true, "conference", default))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _engagementManager.GetAllAsync(1, 10, "startdate", true, "conference");

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(1);
        _repository.Verify(r => r.GetAllAsync(1, 10, "startdate", true, "conference", default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithSortAndFilter_CallsDataStoreWithBothParams()
    {
        // Arrange
        var pagedResult = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Code Conference" } },
            TotalCount = 1
        };
        _repository
            .Setup(r => r.GetAllAsync(2, 20, "enddate", false, "code", default))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _engagementManager.GetAllAsync(2, 20, "enddate", false, "code");

        // Assert
        result.Should().NotBeNull();
        _repository.Verify(r => r.GetAllAsync(2, 20, "enddate", false, "code", default), Times.Once);
    }
}