using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SpeakingEngagement;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadAllSpeakingEngagementsTests
{
    private readonly Mock<ISpeakingEngagementsReader> _speakingEngagementsReader;
    private readonly Mock<IEngagementManager> _engagementManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly LoadAllSpeakingEngagements _sut;

    public LoadAllSpeakingEngagementsTests()
    {
        _speakingEngagementsReader = new Mock<ISpeakingEngagementsReader>();
        _engagementManager = new Mock<IEngagementManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();

        _sut = new LoadAllSpeakingEngagements(
            _speakingEngagementsReader.Object,
            _engagementManager.Object,
            _feedCheckManager.Object,
            NullLogger<LoadAllSpeakingEngagements>.Instance);
    }

    private static Engagement CreateEngagement(string name = "Test Conference", string url = "https://example.com/event", int year = 2024) =>
        new Engagement
        {
            Id = 0,
            Name = name,
            Url = url,
            StartDateTime = new DateTime(year, 6, 15),
            EndDateTime = new DateTime(year, 6, 17),
            TimeZoneId = "America/Phoenix",
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadAllSpeakingEngagements",
                LastCheckedFeed = DateTimeOffset.UtcNow,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                LastUpdatedOn = DateTimeOffset.UtcNow
            });

    private static HttpRequest CreateHttpRequest(string? checkFrom = null)
    {
        var context = new DefaultHttpContext();
        if (checkFrom != null)
        {
            context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "checkFrom", checkFrom }
            });
        }
        return context.Request;
    }

    [Fact]
    public async Task RunAsync_SavesEngagements_WhenEngagementsAreFound()
    {
        // Arrange
        var item = CreateEngagement("DevConf 2024", "https://devconf.com/2024", 2024);
        var savedItem = CreateEngagement("DevConf 2024", "https://devconf.com/2024", 2024);
        savedItem.Id = 42;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement> { item });
        _engagementManager.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(savedItem));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_DoesNotCheckForDuplicates_WhenSavingEngagements()
    {
        // Arrange - LoadAllSpeakingEngagements does NOT check for duplicates (unlike LoadNewSpeakingEngagements)
        var item = CreateEngagement("Conference", "https://conf.com", 2024);
        var savedItem = CreateEngagement("Conference", "https://conf.com", 2024);
        savedItem.Id = 50;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement> { item });
        _engagementManager.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(savedItem));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        // Verify that GetByNameAndUrlAndYearAsync is NEVER called (no deduplication)
        _engagementManager.Verify(m => m.GetByNameAndUrlAndYearAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoEngagementsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ParsesCheckFromParameter_WhenValidDateProvided()
    {
        // Arrange
        var checkFromDate = "2024-01-15";
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement>());

        var request = CreateHttpRequest(checkFromDate);

        // Act
        var result = await _sut.RunAsync(request, checkFromDate);

        // Assert
        _speakingEngagementsReader.Verify(r => r.GetAll(It.Is<DateTimeOffset>(d => d.Year == 2024 && d.Month == 1 && d.Day == 15)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsNull()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        _speakingEngagementsReader.Verify(r => r.GetAll(It.Is<DateTimeOffset>(d => d == DateTimeOffset.MinValue || d == DateTime.MinValue)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsInvalid()
    {
        // Arrange
        var invalidCheckFrom = "not-a-date";
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement>());

        var request = CreateHttpRequest(invalidCheckFrom);

        // Act
        var result = await _sut.RunAsync(request, invalidCheckFrom);

        // Assert
        _speakingEngagementsReader.Verify(r => r.GetAll(It.Is<DateTimeOffset>(d => d == DateTimeOffset.MinValue || d == DateTime.MinValue)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_HandlesMultipleEngagements_Successfully()
    {
        // Arrange
        var engagement1 = CreateEngagement("Conf A", "https://a.com", 2024);
        var engagement2 = CreateEngagement("Conf B", "https://b.com", 2024);
        var engagement3 = CreateEngagement("Conf C", "https://c.com", 2024);

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { engagement1, engagement2, engagement3 });
        
        var saved1 = CreateEngagement("Conf A", "https://a.com", 2024);
        saved1.Id = 1;
        var saved2 = CreateEngagement("Conf B", "https://b.com", 2024);
        saved2.Id = 2;
        var saved3 = CreateEngagement("Conf C", "https://c.com", 2024);
        saved3.Id = 3;
        
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf A"))).ReturnsAsync(OperationResult<Engagement>.Success(saved1));
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf B"))).ReturnsAsync(OperationResult<Engagement>.Success(saved2));
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf C"))).ReturnsAsync(OperationResult<Engagement>.Success(saved3));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Exactly(3));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("3", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ContinuesOnError_WhenSingleEngagementFails()
    {
        // Arrange
        var engagement1 = CreateEngagement("Conf A", "https://a.com", 2024);
        var engagement2 = CreateEngagement("Conf B", "https://b.com", 2024);

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { engagement1, engagement2 });
        
        // First engagement fails to save
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf A")))
            .ThrowsAsync(new Exception("Save failed"));
        
        // Second engagement saves successfully
        var saved2 = CreateEngagement("Conf B", "https://b.com", 2024);
        saved2.Id = 2;
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf B"))).ReturnsAsync(OperationResult<Engagement>.Success(saved2));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        // Should still return OK and continue processing despite failure
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
        Assert.Contains("2", okResult.Value!.ToString()); // 1 of 2 engagements
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Reader error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenFeedCheckManagerThrowsException()
    {
        // Arrange
        var engagement = CreateEngagement("Test Conf", "https://test.com", 2024);
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<Engagement> { engagement });
        _engagementManager.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(engagement));
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>())).ThrowsAsync(new Exception("FeedCheck error"));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("FeedCheck error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_HandlesNullEngagementsList_Gracefully()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _speakingEngagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync((List<Engagement>)null!);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }
}

