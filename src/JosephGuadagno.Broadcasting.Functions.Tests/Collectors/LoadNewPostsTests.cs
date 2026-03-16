using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadNewPostsTests
{
    private readonly Mock<ISyndicationFeedReader> _feedReader;
    private readonly Mock<ISettings> _settings;
    private readonly Mock<ISyndicationFeedSourceManager> _feedSourceManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly LoadNewPosts _sut;

    public LoadNewPostsTests()
    {
        _feedReader = new Mock<ISyndicationFeedReader>();
        _settings = new Mock<ISettings>();
        _feedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _urlShortener = new Mock<IUrlShortener>();
        _eventPublisher = new Mock<IEventPublisher>();

        _settings.Setup(s => s.ShortenedDomainToUse).Returns("short.example.com");
        _eventPublisher.Setup(e => e.PublishSyndicationFeedEventsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SyndicationFeedSource>>()))
            .ReturnsAsync(true);

        _sut = new LoadNewPosts(
            _feedReader.Object,
            _settings.Object,
            _feedSourceManager.Object,
            _feedCheckManager.Object,
            _urlShortener.Object,
            _eventPublisher.Object,
            NullLogger<LoadNewPosts>.Instance);
    }

    private static SyndicationFeedSource CreateFeedSource(string feedIdentifier = "feed-abc") =>
        new SyndicationFeedSource
        {
            Id = 0,
            FeedIdentifier = feedIdentifier,
            Title = "Test Post",
            Author = "Test Author",
            Url = "https://example.com/post",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetupFeedCheck() =>
        _feedCheckManager.Setup(f => f.GetByNameAsync(It.IsAny<string>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = "LoadNewPosts",
                LastCheckedFeed = DateTimeOffset.UtcNow,
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                LastUpdatedOn = DateTimeOffset.UtcNow
            });

    [Fact]
    public async Task RunAsync_SkipsDuplicate_WhenFeedIdentifierAlreadyExists()
    {
        // Arrange
        var item = CreateFeedSource("duplicate-feed-id");
        var existingItem = CreateFeedSource("duplicate-feed-id");
        existingItem.Id = 99;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _feedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("duplicate-feed-id")).ReturnsAsync(existingItem);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
        _eventPublisher.Verify(
            e => e.PublishSyndicationFeedEventsAsync(It.IsAny<string>(), It.Is<IReadOnlyCollection<SyndicationFeedSource>>(l => l.Count == 0)),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_SavesItem_WhenFeedIdentifierIsNew()
    {
        // Arrange
        var item = CreateFeedSource("brand-new-feed-id");
        var savedItem = CreateFeedSource("brand-new-feed-id");
        savedItem.Id = 42;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _feedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("brand-new-feed-id")).ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/abc");
        _feedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(savedItem);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Once);
        _eventPublisher.Verify(
            e => e.PublishSyndicationFeedEventsAsync(It.IsAny<string>(), It.Is<IReadOnlyCollection<SyndicationFeedSource>>(l => l.Count == 1)),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoNewItemsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(new FeedCheck());
        _feedReader.Setup(r => r.GetAsync(It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }
}
