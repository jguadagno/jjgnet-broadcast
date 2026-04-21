using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Collectors.SyndicationFeed;
using JosephGuadagno.Broadcasting.Functions.Interfaces;
using JosephGuadagno.Broadcasting.Functions.Models;
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
    private readonly Mock<ISyndicationFeedReader> _feedReader;
    private readonly Mock<ISyndicationFeedSourceManager> _feedSourceManager;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly Mock<IUrlShortener> _urlShortener;
    private readonly Mock<IEventPublisher> _eventPublisher;
    private readonly LoadNewPosts _sut;

    public LoadNewPostsTests()
    {
        _feedReader = new Mock<ISyndicationFeedReader>();
        _feedSourceManager = new Mock<ISyndicationFeedSourceManager>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _urlShortener = new Mock<IUrlShortener>();
        _eventPublisher = new Mock<IEventPublisher>();

        _eventPublisher.Setup(e => e.PublishSyndicationFeedEventsAsync(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<SyndicationFeedSource>>()))
            .Returns(Task.CompletedTask);
        _feedSourceManager.Setup(m => m.GetCollectorOwnerOidAsync(It.IsAny<CancellationToken>())).ReturnsAsync(OwnerEntraOid);

        _sut = new LoadNewPosts(
            _feedReader.Object,
            Options.Create(new Settings { ShortenedDomainToUse = "short.example.com", OwnerEntraOid = OwnerEntraOid }),
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
            LastUpdatedOn = DateTimeOffset.UtcNow,
            CreatedByEntraOid = ""
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
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
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("brand-new-feed-id")).ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/abc");
        _feedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedItem));

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
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource> { item });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("owned-feed-id"))
            .ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://short.example.com/owned");
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p =>
                p.FeedIdentifier == "owned-feed-id" &&
                p.CreatedByEntraOid == CollectorOwnerEntraOid &&
                !string.IsNullOrWhiteSpace(p.CreatedByEntraOid))))
            .ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedItem));

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p =>
            p.FeedIdentifier == "owned-feed-id" &&
            p.CreatedByEntraOid == CollectorOwnerEntraOid &&
            !string.IsNullOrWhiteSpace(p.CreatedByEntraOid))), Times.Once);
    }

    [Fact]
    public async Task RunAsync_UsesCollectorOwnerOid_WhenReadingNewItems()
    {
        // Arrange
        SetupFeedCheck();
        _feedSourceManager.Setup(m => m.GetCollectorOwnerOidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CollectorOwnerEntraOid);
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource>());

        // Act
        await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.GetCollectorOwnerOidAsync(It.IsAny<CancellationToken>()), Times.Once);
        _feedReader.Verify(r => r.GetAsync(CollectorOwnerEntraOid, It.IsAny<DateTimeOffset>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ReturnsOk_WhenNoNewItemsFound()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource>());

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
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
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource> { newPost1, duplicatePost, newPost2 });
        
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("new-1")).ReturnsAsync((SyndicationFeedSource?)null);
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("new-2")).ReturnsAsync((SyndicationFeedSource?)null);
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("duplicate-1")).ReturnsAsync(existingPost);
        
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("https://short.example.com/xyz");
        
        var savedPost1 = CreateFeedSource("new-1");
        savedPost1.Id = 1;
        var savedPost2 = CreateFeedSource("new-2");
        savedPost2.Id = 2;
        
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "new-1"))).ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedPost1));
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "new-2"))).ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedPost2));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Exactly(2));
        _eventPublisher.Verify(
            e => e.PublishSyndicationFeedEventsAsync(It.IsAny<string>(), It.Is<IReadOnlyCollection<SyndicationFeedSource>>(l => l.Count == 2)),
            Times.Once);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("2", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenReaderThrowsException()
    {
        // Arrange
        SetupFeedCheck();
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ThrowsAsync(new Exception("Reader error"));

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
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync(new List<SyndicationFeedSource> { item });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("post-456")).ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(item.Url, "short.example.com")).ReturnsAsync("https://short.example.com/abc");
        _feedSourceManager.Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _urlShortener.Verify(u => u.GetShortenedUrlAsync(item.Url, "short.example.com"), Times.Once);
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.ShortenedUrl == "https://short.example.com/abc")), Times.Once);
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
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>()))
            .ReturnsAsync(new List<SyndicationFeedSource> { failedItem, successfulItem });
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("transaction-failure"))
            .ReturnsAsync((SyndicationFeedSource?)null);
        _feedSourceManager.Setup(m => m.GetByFeedIdentifierAsync("transaction-success"))
            .ReturnsAsync((SyndicationFeedSource?)null);
        _urlShortener.Setup(u => u.GetShortenedUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://short.example.com/txn");
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "transaction-failure")))
            .ThrowsAsync(new InvalidOperationException("The configured execution strategy 'SqlServerRetryingExecutionStrategy' does not support user-initiated transactions."));
        _feedSourceManager.Setup(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "transaction-success")))
            .ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(savedItem));

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "transaction-failure")), Times.Exactly(4));
        _feedSourceManager.Verify(m => m.SaveAsync(It.Is<SyndicationFeedSource>(p => p.FeedIdentifier == "transaction-success")), Times.Once);
        _eventPublisher.Verify(
            e => e.PublishSyndicationFeedEventsAsync(
                It.IsAny<string>(),
                It.Is<IReadOnlyCollection<SyndicationFeedSource>>(items =>
                    items.Count == 1 && items.Single().FeedIdentifier == "transaction-success")),
            Times.Once);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("Loaded 1 of 2 post(s)", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_HandlesNullFeedList_Gracefully()
    {
        // Arrange
        SetupFeedCheck();
        _feedCheckManager.Setup(f => f.SaveAsync(It.IsAny<FeedCheck>())).ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));
        _feedReader.Setup(r => r.GetAsync(OwnerEntraOid, It.IsAny<DateTimeOffset>())).ReturnsAsync((List<SyndicationFeedSource>?)null);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedSourceManager.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("0", okResult.Value!.ToString());
    }

    [Fact]
    public async Task RunAsync_ReturnsBadRequest_WhenCollectorOwnerOidCannotBeResolved()
    {
        // Arrange
        SetupFeedCheck();
        _feedSourceManager.Setup(m => m.GetCollectorOwnerOidAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.RunAsync(null!);

        // Assert
        _feedReader.Verify(r => r.GetAsync(It.IsAny<string>(), It.IsAny<DateTimeOffset>()), Times.Never);
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("owner OID", badRequestResult.Value!.ToString());
    }
}
