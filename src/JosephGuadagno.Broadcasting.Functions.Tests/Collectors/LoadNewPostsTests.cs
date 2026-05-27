using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;
using JosephGuadagno.Broadcasting.Functions.Models;
using JosephGuadagno.Broadcasting.Functions.Services;
using JosephGuadagno.Broadcasting.SyndicationFeedReader.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Collectors;

public class LoadNewPostsTests
{
    private const string OwnerEntraOid = "owner-entra-oid";
    private const string CollectorOwnerEntraOid = "collector-owner-entra-oid";
    private const string TestFeedUrl = "http://test-feed";
    private readonly Mock<ISyndicationFeedReader> _feedReader;
    private readonly Mock<ISyndicationFeedItemManager> _feedSourceManager;
    private readonly Mock<IUserCollectorFeedSourceManager> _userCollectorFeedSourceManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly Mock<ICollectorEventDispatcher> _collectorEventDispatcher;
    private readonly LoadNewPosts _sut;

    public LoadNewPostsTests()
    {
        _feedReader = new Mock<ISyndicationFeedReader>();
        _feedSourceManager = new Mock<ISyndicationFeedItemManager>();
        _userCollectorFeedSourceManager = new Mock<IUserCollectorFeedSourceManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _urlShortener = new Mock<IUrlShortener>();
        _collectorEventDispatcher = new Mock<ICollectorEventDispatcher>();

        _userCollectorFeedSourceManager.Setup(m => m.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserCollectorFeedSource>
            {
                new UserCollectorFeedSource { CreatedByEntraOid = OwnerEntraOid, FeedUrl = TestFeedUrl, IsActive = true }
            });

        _sut = new LoadNewPosts(
            _feedReader.Object,
            Options.Create(new Settings { ShortenedDomainToUse = "short.example.com", OwnerEntraOid = OwnerEntraOid }),
            _feedSourceManager.Object,
            _userCollectorFeedSourceManager.Object,
            _feedCheckManager.Object,
            _urlShortener.Object,
            _collectorEventDispatcher.Object,
            NullLogger<LoadNewPosts>.Instance);
    }

    private static SyndicationFeedItem CreateFeedSource(string feedIdentifier = "feed-abc") =>
        new SyndicationFeedItem
        {
            Id = 0,
            FeedIdentifier = feedIdentifier,
            Title = "Test Post",
            Author = "Test Author",
            Url = "https://example.com/post",
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedItem> { item });
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("duplicate-feed-id", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>()), Times.Never);
        _collectorEventDispatcher.Verify(
            e => e.DispatchSyndicationFeedItemAsync(It.IsAny<SyndicationFeedItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedItem> { item });
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("brand-new-feed-id", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/abc");
        _feedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>())).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>()), Times.Once);
        _collectorEventDispatcher.Verify(
            e => e.DispatchSyndicationFeedItemAsync(It.IsAny<SyndicationFeedItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("1", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_PreservesReaderOwnerOid_WhenSavingNewItem()
    {
        // Arrange
        var item = CreateFeedSource("owned-feed-id");
        item.CreatedByEntraOid = CollectorOwnerEntraOid;

        var savedItem = CreateFeedSource("owned-feed-id");
        savedItem.Id = 42;
        savedItem.CreatedByEntraOid = CollectorOwnerEntraOid;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedItem> { item });
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("owned-feed-id", OwnerEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://short.example.com/owned");
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p =>
                p.FeedIdentifier == "owned-feed-id" &&
                p.CreatedByEntraOid == CollectorOwnerEntraOid &&
                !string.IsNullOrWhiteSpace(p.CreatedByEntraOid))))
            .ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedItem));

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p =>
            p.FeedIdentifier == "owned-feed-id" &&
            p.CreatedByEntraOid == CollectorOwnerEntraOid &&
            !string.IsNullOrWhiteSpace(p.CreatedByEntraOid))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_UsesCollectorOwnerOid_WhenReadingNewItems()
    {
        // Arrange
        SetupFeedCheck();
        _userCollectorFeedSourceManager.Setup(m => m.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserCollectorFeedSource>
            {
                new UserCollectorFeedSource { CreatedByEntraOid = CollectorOwnerEntraOid, FeedUrl = TestFeedUrl, IsActive = true }
            });
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedItem>());

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _userCollectorFeedSourceManager.Verify(m => m.GetAllActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _feedReader.Verify(r => r.GetAsync(It.IsAny<string>(), CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoNewItemsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedItem>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedItem> { newPost1, duplicatePost, newPost2 });
        
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("new-1", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("new-2", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("duplicate-1", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        var savedPost1 = CreateFeedSource("new-1");
        savedPost1.Id = 1;
        var savedPost2 = CreateFeedSource("new-2");
        savedPost2.Id = 2;
        
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "new-1"))).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedPost1));
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "new-2"))).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedPost2));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>()), Times.Exactly(2));
        _collectorEventDispatcher.Verify(
            e => e.DispatchSyndicationFeedItemAsync(It.IsAny<SyndicationFeedItem>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

        // Act
        var result = await _sut.RunAsync(null!);

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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedItem> { item });
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("post-456", OwnerEntraOid, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(item.Url, "short.example.com")).ReturnsAsync("https://short.example.com/abc");
        _feedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedItem>())).ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _urlShortener.Verify(u => u.GetShortenedUrlAsync(item.Url, "short.example.com"), Times.Once);
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.ShortenedUrl == "https://short.example.com/abc")), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ContinuesWhenSaveThrowsExecutionStrategyTransactionError()
    {
        // Arrange
        var failedItem = CreateFeedSource("transaction-failure");
        var successfulItem = CreateFeedSource("transaction-success");
        var savedItem = CreateFeedSource("transaction-success");
        savedItem.Id = 77;

        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(It.IsAny<string>(), OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedItem> { failedItem, successfulItem });
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("transaction-failure", OwnerEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _feedSourceManager.Setup(m => m.IsFeedItemUniqueToUser("transaction-success", OwnerEntraOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://short.example.com/txn");
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "transaction-failure")))
            .ThrowsAsync(new InvalidOperationException("The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions."));
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "transaction-success")))
            .ReturnsAsync(OperationResult<SyndicationFeedItem>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "transaction-failure")), Times.Exactly(4));
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedItem>(p => p.FeedIdentifier == "transaction-success")), Times.Once);
        _collectorEventDispatcher.Verify(
            e => e.DispatchSyndicationFeedItemAsync(
                It.Is<SyndicationFeedItem>(item => item.FeedIdentifier == "transaction-success"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Loaded 1 of 2 post(s)", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoActiveConfigsFound()
    {
        // Arrange
        SetupFeedCheck();
        _userCollectorFeedSourceManager.Setup(m => m.GetAllActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserCollectorFeedSource>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedReader.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("No active", okResult.Value!.ToString());
    }
}
