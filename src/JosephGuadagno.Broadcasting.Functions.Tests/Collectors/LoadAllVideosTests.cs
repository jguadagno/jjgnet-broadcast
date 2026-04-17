using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.YouTube;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.YouTubeReader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadAllVideosTests
{
    private readonly Mock<IYouTubeReader> _youTubeReader;
    private readonly Mock<IYouTubeSourceManager> _youTubeSourceManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly LoadAllVideos _sut;

    public LoadAllVideosTests()
    {
        _youTubeReader = new Mock<IYouTubeReader>();
        _youTubeSourceManager = new Mock<IYouTubeSourceManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _urlShortener = new Mock<IUrlShortener>();

        _sut = new LoadAllVideos(
            _youTubeReader.Object,
            Options.Create(new Settings { ShortenedDomainToUse = "short.example.com" }),
            _youTubeSourceManager.Object,
            _feedCheckManager.Object,
            _urlShortener.Object,
            NullLogger<LoadAllVideos>.Instance);
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
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadAllVideos",
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
    public async Task RunAsync_SavesVideos_WhenVideosAreFound()
    {
        // Arrange
        var item = CreateVideoSource("new-video-id");
        var savedItem = CreateVideoSource("new-video-id");
        savedItem.Id = 42;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource> { item });
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("new-video-id")).ReturnsAsync((YouTubeSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        _youTubeSourceManager.Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>())).ReturnsAsync(OperationResult<YouTubeSource>.Success(savedItem));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_SkipsDuplicate_WhenVideoIdAlreadyExists()
    {
        // Arrange
        var item = CreateVideoSource("duplicate-video-id");
        var existingItem = CreateVideoSource("duplicate-video-id");
        existingItem.Id = 77;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource> { item });
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("duplicate-video-id")).ReturnsAsync(existingItem);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoVideosFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Never);
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
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource>());

        var request = CreateHttpRequest(checkFromDate);

        // Act
        var result = await _sut.RunAsync(request, checkFromDate);

        // Assert
        _youTubeReader.Verify(r => r.GetAsync(It.Is<DateTimeOffset>(d => d.Year == 2024 && d.Month == 1 && d.Day == 15)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsNull()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _youTubeReader.Verify(r => r.GetAsync(It.Is<DateTimeOffset>(d => d == DateTimeOffset.MinValue || d == DateTime.MinValue)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsInvalid()
    {
        // Arrange
        var invalidCheckFrom = "not-a-date";
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource>());

        var request = CreateHttpRequest(invalidCheckFrom);

        // Act
        var result = await _sut.RunAsync(request, invalidCheckFrom);

        // Assert
        _youTubeReader.Verify(r => r.GetAsync(It.Is<DateTimeOffset>(d => d == DateTimeOffset.MinValue || d == DateTime.MinValue)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
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
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<YouTubeSource> { newVideo1, duplicateVideo, newVideo2 });
        
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("new-1")).ReturnsAsync((YouTubeSource?)null);
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("new-2")).ReturnsAsync((YouTubeSource?)null);
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("duplicate-1")).ReturnsAsync(existingVideo);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        var savedVideo1 = CreateVideoSource("new-1");
        savedVideo1.Id = 1;
        var savedVideo2 = CreateVideoSource("new-2");
        savedVideo2.Id = 2;
        
        _youTubeSourceManager.Setup(m => m.SaveAsync(It.Is<YouTubeSource>(v => v.VideoId == "new-1"))).ReturnsAsync(OperationResult<YouTubeSource>.Success(savedVideo1));
        _youTubeSourceManager.Setup(m => m.SaveAsync(It.Is<YouTubeSource>(v => v.VideoId == "new-2"))).ReturnsAsync(OperationResult<YouTubeSource>.Success(savedVideo2));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>()), Times.Exactly(2));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
        Assert.Contains("3", okResult.Value!.ToString()); // 2 of 3 videos
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Reader error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_CallsUrlShortener_WhenSavingVideo()
    {
        // Arrange
        var item = CreateVideoSource("video-123");
        var savedItem = CreateVideoSource("video-123");
        savedItem.Id = 50;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _youTubeReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<YouTubeSource> { item });
        _youTubeSourceManager.Setup(m => m.GetByVideoIdAsync("video-123")).ReturnsAsync((YouTubeSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(item.Url, "short.example.com")).ReturnsAsync("https://short.example.com/abc");
        _youTubeSourceManager.Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>())).ReturnsAsync(OperationResult<YouTubeSource>.Success(savedItem));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _urlShortener.Verify(u => u.GetShortenedUrlAsync(item.Url, "short.example.com"), Times.Once);
        _youTubeSourceManager.Verify(m => m.SaveAsync(It.Is<YouTubeSource>(v => v.ShortenedUrl == "https://short.example.com/abc")), Times.Once);
    }
}

