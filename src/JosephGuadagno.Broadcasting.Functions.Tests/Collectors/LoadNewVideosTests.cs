using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadNewVideosTests
{
    private readonly Mock<IYouTubeReader> _youTubeReader;
    private readonly Mock<ISettings> _settings;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IYouTubeSourceManager> _youTubeSourceManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly LoadNewVideos _sut;

    public LoadNewVideosTests()
    {
        _youTubeReader = new Mock<IYouTubeReader>();
        _settings = new Mock<ISettings>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _youTubeSourceManager = new Mock<IYouTubeSourceManager>();
        _urlShortener = new Mock<IUrlShortener>();
        _eventPublisher = new Mock<IEventPublisher>();

        _settings.Setup(s => s.ShortenedDomainToUse).Returns("short.example.com");
        _eventPublisher.Setup(e => e.PublishYouTubeEventsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<YouTubeSource>>()))
            .ReturnsAsync(true);

        _sut = new LoadNewVideos(
            _youTubeReader.Object,
            _settings.Object,
            _feedCheckManager.Object,
            _youTubeSourceManager.Object,
            _urlShortener.Object,
            _eventPublisher.Object,
            NullLogger<LoadNewVideos>.Instance);
    }

    private static YouTubeSource CreateVideoSource(string videoId = "abc123") =>
        new YouTubeSource
        {
            Id = 0,
            VideoId = videoId,
            Title = "Test Video",
            Author = "Test Channel",
            Url = $"https://youtube.com/watch?v={videoId}",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadNewVideos",
                LastCheckedFeed = DateTimeOffset.UtcNow,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                LastUpdatedOn = DateTimeOffset.UtcNow
            });

    [Fact]
    public async Task RunAsync_SkipsDuplicate_WhenVideoIdAlreadyExists()
    {
        // Arrange
        var item = CreateVideoSource("duplicate-video-id");
        var existingItem = CreateVideoSource("duplicate-video-id");
        existingItem.Id = 77;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource> { item });
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("duplicate-video-id")).ReturnsAsync(existingItem);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Never);
        _eventPublisher.Verify(
            e => e.PublishYouTubeEventsAsync(It.IsAny<string>(), It.Is<IReadOnlyCollection<YouTubeSource>>(l => l.Count == 0)),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_SavesVideo_WhenVideoIdIsNew()
    {
        // Arrange
        var item = CreateVideoSource("brand-new-video-id");
        var savedItem = CreateVideoSource("brand-new-video-id");
        savedItem.Id = 55;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource> { item });
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("brand-new-video-id")).ReturnsAsync((YouTubeSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        _youTubeSourceManager.Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>())).ReturnsAsync(savedItem);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Once);
        _eventPublisher.Verify(
            e => e.PublishYouTubeEventsAsync(It.IsAny<string>(), It.Is<IReadOnlyCollection<YouTubeSource>>(l => l.Count == 1)),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoNewVideosFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }
}
