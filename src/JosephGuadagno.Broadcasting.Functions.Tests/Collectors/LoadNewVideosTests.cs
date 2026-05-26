using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadNewVideosTests
{
    private const string OwnerEntraOid = "owner-entra-oid";
    private const string CollectorOwnerEntraOid = "collector-owner-entra-oid";
    private readonly Mock<IYouTubeReader> _youTubeReader;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUserCollectorYouTubeChannelManager> _userCollectorYouTubeChannelManager;
    private readonly Mock<IYouTubeItemManager> _youTubeItemManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly Mock<ICollectorEventPublisher> _collectorEventPublisher;
    private readonly LoadNewVideos _sut;

    public LoadNewVideosTests()
    {
        _youTubeReader = new Mock<IYouTubeReader>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _userCollectorYouTubeChannelManager = new Mock<IUserCollectorYouTubeChannelManager>();
        _youTubeItemManager = new Mock<IYouTubeItemManager>();
        _urlShortener = new Mock<IUrlShortener>();
        _collectorEventPublisher = new Mock<ICollectorEventPublisher>();

        _userCollectorYouTubeChannelManager.Setup(m => m.GetAllActiveAsync())
            .ReturnsAsync(new List<UserCollectorYouTubeChannel>
            {
                new UserCollectorYouTubeChannel { CreatedByEntraOid = OwnerEntraOid, ChannelId = "test-channel", IsActive = true }
            });
        _userCollectorYouTubeChannelManager
            .Setup(m => m.GetApiKeyAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("test-api-key");

        _sut = new LoadNewVideos(
            _youTubeReader.Object,
            Options.Create(new Settings { ShortenedDomainToUse = "short.example.com", OwnerEntraOid = OwnerEntraOid }),
            _feedCheckManager.Object,
            _userCollectorYouTubeChannelManager.Object,
            _youTubeItemManager.Object,
            _urlShortener.Object,
            _collectorEventPublisher.Object,
            NullLogger<LoadNewVideos>.Instance);
    }

    private static YouTubeItem CreateVideoSource(string videoId = "abc123") =>
        new YouTubeItem
        {
            Id = 0,
            VideoId = videoId,
            Title = "Test Video",
            Author = "Test Channel",
            Url = $"https://youtube.com/watch?v={videoId}",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ReturnsAsync(new List<YouTubeItem> { item });
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("duplicate-video-id", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeItem>()), Times.Never);
        _collectorEventPublisher.Verify(
            e => e.PublishYouTubeItemAsync(It.IsAny<YouTubeItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ReturnsAsync(new List<YouTubeItem> { item });
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("brand-new-video-id", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        _youTubeItemManager.Setup(m => m.SaveAsync(It.IsAny<YouTubeItem>())).ReturnsAsync(OperationResult<YouTubeItem>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeItem>()), Times.Once);
        _collectorEventPublisher.Verify(
            e => e.PublishYouTubeItemAsync(It.IsAny<YouTubeItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_PreservesReaderOwnerOid_WhenSavingVideo()
    {
        // Arrange
        var item = CreateVideoSource("owned-video-id");
        item.CreatedByEntraOid = CollectorOwnerEntraOid;

        var savedItem = CreateVideoSource("owned-video-id");
        savedItem.Id = 55;
        savedItem.CreatedByEntraOid = CollectorOwnerEntraOid;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>()))
            .ReturnsAsync(new List<YouTubeItem> { item });
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("owned-video-id", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://short.example.com/owned");
        _youTubeItemManager.Setup(m => m.SaveAsync(It.Is<YouTubeItem>(v =>
                v.VideoId == "owned-video-id" &&
                v.CreatedByEntraOid == CollectorOwnerEntraOid &&
                !string.IsNullOrWhiteSpace(v.CreatedByEntraOid))))
            .ReturnsAsync(OperationResult<YouTubeItem>.Success(savedItem));

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.Is<YouTubeItem>(v =>
            v.VideoId == "owned-video-id" &&
            v.CreatedByEntraOid == CollectorOwnerEntraOid &&
            !string.IsNullOrWhiteSpace(v.CreatedByEntraOid))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_UsesCollectorOwnerOid_WhenReadingNewVideos()
    {
        // Arrange
        SetupFeedCheck();
        _userCollectorYouTubeChannelManager.Setup(m => m.GetAllActiveAsync())
            .ReturnsAsync(new List<UserCollectorYouTubeChannel>
            {
                new UserCollectorYouTubeChannel { CreatedByEntraOid = CollectorOwnerEntraOid, ChannelId = "test-channel", IsActive = true }
            });
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>()))
            .ReturnsAsync(new List<YouTubeItem>());

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _userCollectorYouTubeChannelManager.Verify(m => m.GetAllActiveAsync(), Times.Once);
        _youTubeReader.Verify(r => r.GetAsync(CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoNewVideosFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ReturnsAsync(new List<YouTubeItem>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeItem>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_HandlesMultipleVideos_WithMixedDuplicates()
    {
        // Arrange
        var newVideo1 = CreateVideoSource("new-1");
        var newVideo2 = CreateVideoSource("new-2");
        var duplicateVideo = CreateVideoSource("duplicate-1");
        var existingVideo = CreateVideoSource("duplicate-1");
        existingVideo.Id = 99;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>()))
            .ReturnsAsync(new List<YouTubeItem> { newVideo1, duplicateVideo, newVideo2 });
        
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("new-1", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("new-2", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("duplicate-1", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        var savedVideo1 = CreateVideoSource("new-1");
        savedVideo1.Id = 1;
        var savedVideo2 = CreateVideoSource("new-2");
        savedVideo2.Id = 2;
        
        _youTubeItemManager.Setup(m => m.SaveAsync(It.Is<YouTubeItem>(v => v.VideoId == "new-1"))).ReturnsAsync(OperationResult<YouTubeItem>.Success(savedVideo1));
        _youTubeItemManager.Setup(m => m.SaveAsync(It.Is<YouTubeItem>(v => v.VideoId == "new-2"))).ReturnsAsync(OperationResult<YouTubeItem>.Success(savedVideo2));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeItem>()), Times.Exactly(2));
        _collectorEventPublisher.Verify(
            e => e.PublishYouTubeItemAsync(It.IsAny<YouTubeItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ThrowsAsync(new Exception("Reader error"));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Reader error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_CallsUrlShortener_WhenSavingVideo()
    {
        // Arrange
        var item = CreateVideoSource("video-789");
        var savedItem = CreateVideoSource("video-789");
        savedItem.Id = 50;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ReturnsAsync(new List<YouTubeItem> { item });
        _youTubeItemManager.Setup(m => m.IsVideoUniqueToUser("video-789", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(item.Url, "short.example.com")).ReturnsAsync("https://short.example.com/abc");
        _youTubeItemManager.Setup(m => m.SaveAsync(It.IsAny<YouTubeItem>())).ReturnsAsync(OperationResult<YouTubeItem>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _urlShortener.Verify(u => u.GetShortenedUrlAsync(item.Url, "short.example.com"), Times.Once);
        _youTubeItemManager.Verify(m => m.SaveAsync(It.Is<YouTubeItem>(v => v.ShortenedUrl == "https://short.example.com/abc")), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderReturnsNull()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>())).ReturnsAsync((List<YouTubeItem>)null!);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeItemManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeItem>()), Times.Never);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoActiveChannelConfigsFound()
    {
        // Arrange
        SetupFeedCheck();
        _userCollectorYouTubeChannelManager.Setup(m => m.GetAllActiveAsync())
            .ReturnsAsync(new List<UserCollectorYouTubeChannel>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _youTubeReader.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>(), It.IsAny<IYouTubeSettings>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("No active", okResult.Value!.ToString());
    }
}
