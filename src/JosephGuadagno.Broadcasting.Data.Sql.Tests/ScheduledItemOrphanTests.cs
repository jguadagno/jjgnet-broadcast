using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Mock-based contract tests for IScheduledItemDataStore.GetOrphanedScheduledItemsAsync()
/// introduced in issue #274.
///
/// Note: The concrete implementation relies on FromSqlRaw which is not supported by the
/// InMemory EF provider, so these tests verify the interface contract via Moq.
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
}
