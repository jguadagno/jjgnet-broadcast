using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using SqlModels = JosephGuadagno.Broadcasting.Data.Sql.Models;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for IScheduledItemDataStore.GetOrphanedScheduledItemsAsync().
///
/// Contract tests use Moq to verify interface-level behaviour.
/// Concrete implementation tests use the EF Core InMemory provider to verify
/// that the LINQ query correctly identifies orphaned scheduled items without
/// loading all parent IDs into memory (issue #298).
/// </summary>
public class ScheduledItemOrphanTests
{
    private static ScheduledItem MakeOrphan(int id, ScheduledItemType itemType, int primaryKey) =>
        new()
        {
            Id = id,
            ItemType = itemType,
            ItemPrimaryKey = primaryKey,
            Message = $"Orphan {id}",
            SendOnDateTime = DateTimeOffset.UtcNow.AddHours(1)
        };

    // ── C. GetOrphanedScheduledItemsAsync ─────────────────────────────────────

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_ReturnsOrphanedItems()
    {
        // Arrange
        var orphans = new List<ScheduledItem>
        {
            MakeOrphan(1, ScheduledItemType.Engagements, 100),
            MakeOrphan(2, ScheduledItemType.Talks, 200),
            MakeOrphan(3, ScheduledItemType.SyndicationFeedSources, 300)
        };

        var mockStore = new Mock<IScheduledItemDataStore>();
        mockStore
            .Setup(s => s.GetOrphanedScheduledItemsAsync())
            .ReturnsAsync(orphans);

        // Act
        var result = (await mockStore.Object.GetOrphanedScheduledItemsAsync()).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        mockStore.Verify(s => s.GetOrphanedScheduledItemsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_ReturnsEmpty_WhenNoOrphans()
    {
        // Arrange
        var mockStore = new Mock<IScheduledItemDataStore>();
        mockStore
            .Setup(s => s.GetOrphanedScheduledItemsAsync())
            .ReturnsAsync(Enumerable.Empty<ScheduledItem>());

        // Act
        var result = (await mockStore.Object.GetOrphanedScheduledItemsAsync()).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        mockStore.Verify(s => s.GetOrphanedScheduledItemsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_ReturnsItemsWithCorrectItemTypes()
    {
        // Arrange — one orphan per item type
        var orphans = new List<ScheduledItem>
        {
            MakeOrphan(1, ScheduledItemType.Engagements, 10),
            MakeOrphan(2, ScheduledItemType.Talks, 20),
            MakeOrphan(3, ScheduledItemType.SyndicationFeedSources, 30),
            MakeOrphan(4, ScheduledItemType.YouTubeSources, 40)
        };

        var mockStore = new Mock<IScheduledItemDataStore>();
        mockStore
            .Setup(s => s.GetOrphanedScheduledItemsAsync())
            .ReturnsAsync(orphans);

        // Act
        var result = (await mockStore.Object.GetOrphanedScheduledItemsAsync()).ToList();

        // Assert — all four ScheduledItemType values are represented
        Assert.Contains(result, r => r.ItemType == ScheduledItemType.Engagements);
        Assert.Contains(result, r => r.ItemType == ScheduledItemType.Talks);
        Assert.Contains(result, r => r.ItemType == ScheduledItemType.SyndicationFeedSources);
        Assert.Contains(result, r => r.ItemType == ScheduledItemType.YouTubeSources);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_OrphanedItem_HasCorrectItemTableName()
    {
        // Verify that orphaned domain items expose the computed ItemTableName correctly
        var orphans = new List<ScheduledItem>
        {
            MakeOrphan(1, ScheduledItemType.Engagements, 99),
            MakeOrphan(2, ScheduledItemType.YouTubeSources, 88)
        };

        var mockStore = new Mock<IScheduledItemDataStore>();
        mockStore
            .Setup(s => s.GetOrphanedScheduledItemsAsync())
            .ReturnsAsync(orphans);

        var result = (await mockStore.Object.GetOrphanedScheduledItemsAsync()).ToList();

        Assert.Equal("Engagements", result[0].ItemTableName);
        Assert.Equal("YouTubeSources", result[1].ItemTableName);
    }

    // ── Concrete implementation tests (EF InMemory) ───────────────────────────

    private static (BroadcastingContext context, ScheduledItemDataStore store) CreateInMemoryStore()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new BroadcastingContext(options);
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfiles.BroadcastingProfile>(),
            new LoggerFactory());
        var logger = new Mock<ILogger<ScheduledItemDataStore>>();
        return (context, new ScheduledItemDataStore(context, config.CreateMapper(), logger.Object));
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_Concrete_ReturnsOrphan_WhenEngagementParentMissing()
    {
        var (context, store) = CreateInMemoryStore();
        context.ScheduledItems.Add(new SqlModels.ScheduledItem
        {
            ItemTableName = "Engagements", ItemPrimaryKey = 999,
            Message = "Orphan", SendOnDateTime = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var result = (await store.GetOrphanedScheduledItemsAsync()).ToList();

        Assert.Single(result);
        Assert.Equal(999, result[0].ItemPrimaryKey);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_Concrete_ExcludesItem_WhenEngagementParentExists()
    {
        var (context, store) = CreateInMemoryStore();
        context.Engagements.Add(new SqlModels.Engagement
        {
            Id = 1, Name = "Conf", Url = "url", TimeZoneId = "UTC",
            StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow
        });
        context.ScheduledItems.Add(new SqlModels.ScheduledItem
        {
            ItemTableName = "Engagements", ItemPrimaryKey = 1,
            Message = "Has parent", SendOnDateTime = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var result = (await store.GetOrphanedScheduledItemsAsync()).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_Concrete_DetectsOrphansAcrossAllFourTypes()
    {
        var (context, store) = CreateInMemoryStore();

        context.Engagements.Add(new SqlModels.Engagement
        {
            Id = 1, Name = "Conf", Url = "url", TimeZoneId = "UTC",
            StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow,
            CreatedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow
        });
        context.Talks.Add(new SqlModels.Talk
        {
            Id = 1, EngagementId = 1, Name = "Talk",
            UrlForConferenceTalk = "url1", UrlForTalk = "url2",
            StartDateTime = DateTimeOffset.UtcNow, EndDateTime = DateTimeOffset.UtcNow
        });

        // Non-orphaned items
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 1, Message = "ok-eng", SendOnDateTime = DateTimeOffset.UtcNow });
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "Talks", ItemPrimaryKey = 1, Message = "ok-talk", SendOnDateTime = DateTimeOffset.UtcNow });

        // Orphaned items — parents don't exist
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 99, Message = "orphan-eng", SendOnDateTime = DateTimeOffset.UtcNow });
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "Talks", ItemPrimaryKey = 99, Message = "orphan-talk", SendOnDateTime = DateTimeOffset.UtcNow });
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "SyndicationFeedSources", ItemPrimaryKey = 99, Message = "orphan-sfs", SendOnDateTime = DateTimeOffset.UtcNow });
        context.ScheduledItems.Add(new SqlModels.ScheduledItem { ItemTableName = "YouTubeSources", ItemPrimaryKey = 99, Message = "orphan-yt", SendOnDateTime = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();

        var result = (await store.GetOrphanedScheduledItemsAsync()).ToList();

        Assert.Equal(4, result.Count);
        Assert.Contains(result, r => r.Message == "orphan-eng");
        Assert.Contains(result, r => r.Message == "orphan-talk");
        Assert.Contains(result, r => r.Message == "orphan-sfs");
        Assert.Contains(result, r => r.Message == "orphan-yt");
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_Concrete_ReturnsEmpty_WhenNoScheduledItems()
    {
        var (_, store) = CreateInMemoryStore();
        var result = (await store.GetOrphanedScheduledItemsAsync()).ToList();
        Assert.Empty(result);
    }
}
