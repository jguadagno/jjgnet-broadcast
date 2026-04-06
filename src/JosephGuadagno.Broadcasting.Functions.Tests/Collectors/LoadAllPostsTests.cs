using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadAllPostsTests
{
    private readonly Mock<ISyndicationFeedReader> _syndicationFeedReader;
    private readonly Mock<ISyndicationFeedSourceManager> _syndicationFeedSourceManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly LoadAllPosts _sut;

    public LoadAllPostsTests()
    {
        _syndicationFeedReader = new Mock<ISyndicationFeedReader>();
        _syndicationFeedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _urlShortener = new Mock<IUrlShortener>();

        _sut = new LoadAllPosts(
            _syndicationFeedReader.Object,
            Options.Create(new Settings { ShortenedDomainToUse = "short.example.com" }),
            _syndicationFeedSourceManager.Object,
            _feedCheckManager.Object,
            _urlShortener.Object,
            NullLogger<LoadAllPosts>.Instance);
    }

    private static SyndicationFeedSource CreateFeedSource(string feedIdentifier = "feed-123") =>
        new SyndicationFeedSource
        {
            Id = 0,
            FeedIdentifier = feedIdentifier,
            Title = "Test Blog Post",
            Author = "Test Author",
            Url = $"https://example.com/posts/{feedIdentifier}",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadAllPosts",
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
    public async Task RunAsync_SavesPosts_WhenPostsAreFound()
    {
        // Arrange
        var item = CreateFeedSource("new-post-123");
        var savedItem = CreateFeedSource("new-post-123");
        savedItem.Id = 42;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("new-post-123")).ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(savedItem);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _syndicationFeedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_SkipsDuplicate_WhenFeedIdentifierAlreadyExists()
    {
        // Arrange
        var item = CreateFeedSource("duplicate-feed-123");
        var existingItem = CreateFeedSource("duplicate-feed-123");
        existingItem.Id = 77;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("duplicate-feed-123")).ReturnsAsync(existingItem);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _syndicationFeedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoPostsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _syndicationFeedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ParsesCheckFromParameter_WhenValidDateProvided()
    {
        // Arrange
        var checkFromDate = "2024-01-15T12:00:00Z";
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        var request = CreateHttpRequest(checkFromDate);

        // Act
        var result = await _sut.RunAsync(request, checkFromDate);

        // Assert
        _syndicationFeedReader.Verify(r => r.GetAsync(It.Is<DateTimeOffset>(d => d.Year == 2024 && d.Month == 1 && d.Day == 15)), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsNull()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _syndicationFeedReader.Verify(r => r.GetAsync(DateTimeOffset.MinValue), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_UsesMinValue_WhenCheckFromIsInvalid()
    {
        // Arrange
        var invalidCheckFrom = "not-a-date";
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        var request = CreateHttpRequest(invalidCheckFrom);

        // Act
        var result = await _sut.RunAsync(request, invalidCheckFrom);

        // Assert
        _syndicationFeedReader.Verify(r => r.GetAsync(DateTimeOffset.MinValue), Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_HandlesMultiplePosts_WithMixedDuplicates()
    {
        // Arrange
        var newPost1 = CreateFeedSource("new-1");
        var newPost2 = CreateFeedSource("new-2");
        var duplicatePost = CreateFeedSource("duplicate-1");
        var existingPost = CreateFeedSource("duplicate-1");
        existingPost.Id = 99;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource> { newPost1, duplicatePost, newPost2 });
        
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("new-1")).ReturnsAsync((SyndicationFeedSource?)null);
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("new-2")).ReturnsAsync((SyndicationFeedSource?)null);
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("duplicate-1")).ReturnsAsync(existingPost);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        var savedPost1 = CreateFeedSource("new-1");
        savedPost1.Id = 1;
        var savedPost2 = CreateFeedSource("new-2");
        savedPost2.Id = 2;
        
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "new-1"))).ReturnsAsync(savedPost1);
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "new-2"))).ReturnsAsync(savedPost2);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _syndicationFeedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Exactly(2));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
        Assert.Contains("3", okResult.Value!.ToString()); // 2 of 3 posts
    }

    [Fact]
    public async Task RunAsync_ContinuesOnError_WhenSinglePostFails()
    {
        // Arrange
        var post1 = CreateFeedSource("post-1");
        var post2 = CreateFeedSource("post-2");

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource> { post1, post2 });
        
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("post-1")).ReturnsAsync((SyndicationFeedSource?)null);
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("post-2")).ReturnsAsync((SyndicationFeedSource?)null);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        // First post fails to save
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "post-1")))
            .ThrowsAsync(new Exception("Save failed"));
        
        // Second post saves successfully
        var savedPost2 = CreateFeedSource("post-2");
        savedPost2.Id = 2;
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "post-2"))).ReturnsAsync(savedPost2);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        // Should still return OK and continue processing despite failure
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
        Assert.Contains("2", okResult.Value!.ToString()); // 1 of 2 posts
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Reader error", badRequestResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_CallsUrlShortener_WhenSavingPost()
    {
        // Arrange
        var item = CreateFeedSource("post-456");
        var savedItem = CreateFeedSource("post-456");
        savedItem.Id = 50;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _syndicationFeedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _syndicationFeedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("post-456")).ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(item.Url, "short.example.com")).ReturnsAsync("https://short.example.com/abc");
        _syndicationFeedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(savedItem);

        var request = CreateHttpRequest();

        // Act
        var result = await _sut.RunAsync(request, null);

        // Assert
        _urlShortener.Verify(u => u.GetShortenedUrlAsync(item.Url, "short.example.com"), Times.Once);
        _syndicationFeedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.ShortenedUrl == "https://short.example.com/abc")), Times.Once);
    }
}
