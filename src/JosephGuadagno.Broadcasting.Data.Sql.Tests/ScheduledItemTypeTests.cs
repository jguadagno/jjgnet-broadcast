using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Models;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for the ScheduledItemType enum values and the ScheduledItem domain model's computed
/// ItemTableName property introduced in issue #274.
/// </summary>
public class ScheduledItemTypeTests
{
    // ── D. Enum value coverage ─────────────────────────────────────────────────

    [Fact]
    public void ScheduledItemType_HasAllExpectedValues()
    {
        var values = Enum.GetValues<ScheduledItemType>();

        Assert.Contains(ScheduledItemType.Engagements, values);
        Assert.Contains(ScheduledItemType.Talks, values);
        Assert.Contains(ScheduledItemType.SyndicationFeedSources, values);
        Assert.Contains(ScheduledItemType.YouTubeSources, values);
        Assert.Equal(4, values.Length);
    }

    [Theory]
    [InlineData(ScheduledItemType.Engagements, "Engagements")]
    [InlineData(ScheduledItemType.Talks, "Talks")]
    [InlineData(ScheduledItemType.SyndicationFeedSources, "SyndicationFeedSources")]
    [InlineData(ScheduledItemType.YouTubeSources, "YouTubeSources")]
    public void ScheduledItemType_ToString_ReturnsExpectedString(ScheduledItemType itemType, string expected)
    {
        Assert.Equal(expected, itemType.ToString());
    }

    // ── A. Domain model computed property ─────────────────────────────────────

    [Theory]
    [InlineData(ScheduledItemType.Engagements, "Engagements")]
    [InlineData(ScheduledItemType.Talks, "Talks")]
    [InlineData(ScheduledItemType.SyndicationFeedSources, "SyndicationFeedSources")]
    [InlineData(ScheduledItemType.YouTubeSources, "YouTubeSources")]
    public void ItemTableName_ReturnsEnumToString(ScheduledItemType itemType, string expected)
    {
        // Arrange
        var scheduledItem = new ScheduledItem
        {
            ItemType = itemType,
            ItemPrimaryKey = 1,
            Message = "Test",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        // Act & Assert
        Assert.Equal(expected, scheduledItem.ItemTableName);
    }

    [Fact]
    public void ItemTableName_IsComputedFromItemType_NotSeparatelySettable()
    {
        // Verify ItemTableName is derived — changing ItemType should change ItemTableName
        var scheduledItem = new ScheduledItem
        {
            ItemType = ScheduledItemType.Engagements,
            ItemPrimaryKey = 1,
            Message = "Test",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        Assert.Equal("Engagements", scheduledItem.ItemTableName);

        scheduledItem.ItemType = ScheduledItemType.Talks;

        Assert.Equal("Talks", scheduledItem.ItemTableName);
    }
}
