using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SpeakingEngagement;
using JosephGuadagno.Broadcasting.SpeakingEngagementsReader.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadNewSpeakingEngagementsTests
{
    private readonly Mock<ISpeakingEngagementsReader> _engagementsReader;
    private readonly Mock<IEngagementManager> _engagementManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly LoadNewSpeakingEngagements _sut;

    public LoadNewSpeakingEngagementsTests()
    {
        _engagementsReader = new Mock<ISpeakingEngagementsReader>();
        _engagementManager = new Mock<IEngagementManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();

        _sut = new LoadNewSpeakingEngagements(
            _engagementsReader.Object,
            _engagementManager.Object,
            _feedCheckManager.Object,
            NullLogger<LoadNewSpeakingEngagements>.Instance);
    }

    private static Engagement CreateEngagement(string name = "Test Conf", string url = "https://example.com/conf", int year = 2025) =>
        new Engagement
        {
            Id = 0,
            Name = name,
            Url = url,
            StartDateTime = new DateTimeOffset(year, 6, 1, 9, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(year, 6, 3, 17, 0, 0, TimeSpan.Zero),
            TimeZoneId = "America/Phoenix",
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadNewSpeakingEngagements",
                LastCheckedFeed = DateTimeOffset.UtcNow,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                LastUpdatedOn = DateTimeOffset.UtcNow
            });

    [Fact]
    public async Task RunAsync_SkipsDuplicate_WhenEngagementAlreadyExists()
    {
        // Arrange
        var item = CreateEngagement();
        var existing = CreateEngagement();
        existing.Id = 7;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { item });
        _engagementManager
            .Setup(m => m.GetByNameAndUrlAndYearAsync(item.Name, item.Url, item.StartDateTime.Year))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_SavesEngagement_WhenEngagementIsNew()
    {
        // Arrange
        var item = CreateEngagement();
        var saved = CreateEngagement();
        saved.Id = 42;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { item });
        _engagementManager
            .Setup(m => m.GetByNameAndUrlAndYearAsync(item.Name, item.Url, item.StartDateTime.Year))
            .ReturnsAsync((Engagement?)null);
        _engagementManager.Setup(m => m.SaveAsync(It.IsAny<Engagement>())).ReturnsAsync(OperationResult<Engagement>.Success(saved));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoEngagementsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_HandlesMultipleEngagements_WithMixedDuplicates()
    {
        // Arrange
        var newEngagement1 = CreateEngagement("Conf A", "https://a.com", 2024);
        var newEngagement2 = CreateEngagement("Conf B", "https://b.com", 2024);
        var duplicateEngagement = CreateEngagement("Conf C", "https://c.com", 2024);
        var existingEngagement = CreateEngagement("Conf C", "https://c.com", 2024);
        existingEngagement.Id = 99;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { newEngagement1, duplicateEngagement, newEngagement2 });
        
        _engagementManager.Setup(m => m.GetByNameAndUrlAndYearAsync("Conf A", "https://a.com", 2024))
            .ReturnsAsync((Engagement?)null);
        _engagementManager.Setup(m => m.GetByNameAndUrlAndYearAsync("Conf B", "https://b.com", 2024))
            .ReturnsAsync((Engagement?)null);
        _engagementManager.Setup(m => m.GetByNameAndUrlAndYearAsync("Conf C", "https://c.com", 2024))
            .ReturnsAsync(existingEngagement);
        
        var saved1 = CreateEngagement("Conf A", "https://a.com", 2024);
        saved1.Id = 1;
        var saved2 = CreateEngagement("Conf B", "https://b.com", 2024);
        saved2.Id = 2;
        
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf A"))).ReturnsAsync(OperationResult<Engagement>.Success(saved1));
        _engagementManager.Setup(m => m.SaveAsync(It.Is<Engagement>(e => e.Name == "Conf B"))).ReturnsAsync(OperationResult<Engagement>.Success(saved2));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Exactly(2));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Reader error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ChecksDeduplicationByCompositeKey_NameUrlAndYear()
    {
        // Arrange
        var item = CreateEngagement("CodeConf", "https://codeconf.com/2024", 2024);
        var existingItem = CreateEngagement("CodeConf", "https://codeconf.com/2024", 2024);
        existingItem.Id = 88;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<Engagement> { item });
        _engagementManager.Setup(m => m.GetByNameAndUrlAndYearAsync("CodeConf", "https://codeconf.com/2024", 2024))
            .ReturnsAsync(existingItem);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.GetByNameAndUrlAndYearAsync("CodeConf", "https://codeconf.com/2024", 2024), Times.Once);
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_HandlesNullEngagementList_Gracefully()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _engagementsReader.Setup(r => r.GetAll(It.IsAny<DateTimeOffset>())).ReturnsAsync((List<Engagement>)null!);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _engagementManager.Verify(m => m.SaveAsync(It.IsAny<Engagement>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }
}
