using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// AutoMapper mapping tests for ScheduledItem ↔ Domain.Models.ScheduledItem
/// introduced in issue #274 (ItemTableName string ↔ ScheduledItemType enum).
/// </summary>
public class ScheduledItemMappingTests
{
    private readonly IMapper _mapper;

    public ScheduledItemMappingTests()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfiles.BroadcastingProfile>(),
            new LoggerFactory());
        _mapper = config.CreateMapper();
    }

    // ── B. EF entity → Domain (string → enum) ─────────────────────────────────

    [Theory]
    [InlineData("Engagements", ScheduledItemType.Engagements)]
    [InlineData("Talks", ScheduledItemType.Talks)]
    [InlineData("SyndicationFeedSources", ScheduledItemType.SyndicationFeedSources)]
    [InlineData("YouTubeSources", ScheduledItemType.YouTubeSources)]
    public void MapToDomain_ConvertsItemTableNameStringToEnum(string itemTableName, ScheduledItemType expectedItemType)
    {
        // Arrange
        var efItem = new Models.ScheduledItem
        {
            Id = 1,
            ItemTableName = itemTableName,
            ItemPrimaryKey = 42,
            Message = "Test Message",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        // Act
        var domainItem = _mapper.Map<Domain.Models.ScheduledItem>(efItem);

        // Assert
        Assert.NotNull(domainItem);
        Assert.Equal(expectedItemType, domainItem.ItemType);
        Assert.Equal(itemTableName, domainItem.ItemTableName);
    }

    // ── B. Domain → EF entity (enum → string) ─────────────────────────────────

    [Theory]
    [InlineData(ScheduledItemType.Engagements, "Engagements")]
    [InlineData(ScheduledItemType.Talks, "Talks")]
    [InlineData(ScheduledItemType.SyndicationFeedSources, "SyndicationFeedSources")]
    [InlineData(ScheduledItemType.YouTubeSources, "YouTubeSources")]
    public void MapToEntity_ConvertsEnumToItemTableNameString(ScheduledItemType itemType, string expectedTableName)
    {
        // Arrange
        var domainItem = new Domain.Models.ScheduledItem
        {
            Id = 1,
            ItemType = itemType,
            ItemPrimaryKey = 42,
            Message = "Test Message",
            SendOnDateTime = DateTimeOffset.UtcNow
        };

        // Act
        var efItem = _mapper.Map<Models.ScheduledItem>(domainItem);

        // Assert
        Assert.NotNull(efItem);
        Assert.Equal(expectedTableName, efItem.ItemTableName);
    }

    [Fact]
    public void MapToDomain_PreservesAllScalarProperties()
    {
        // Arrange
        var sendOn = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var sentOn = new DateTimeOffset(2025, 6, 15, 11, 0, 0, TimeSpan.Zero);
        var efItem = new Models.ScheduledItem
        {
            Id = 99,
            ItemTableName = "Talks",
            ItemPrimaryKey = 77,
            Message = "Round-trip test",
            SendOnDateTime = sendOn,
            MessageSent = true,
            MessageSentOn = sentOn
        };

        // Act
        var domainItem = _mapper.Map<Domain.Models.ScheduledItem>(efItem);

        // Assert
        Assert.Equal(99, domainItem.Id);
        Assert.Equal(ScheduledItemType.Talks, domainItem.ItemType);
        Assert.Equal(77, domainItem.ItemPrimaryKey);
        Assert.Equal("Round-trip test", domainItem.Message);
        Assert.Equal(sendOn, domainItem.SendOnDateTime);
        Assert.True(domainItem.MessageSent);
        Assert.Equal(sentOn, domainItem.MessageSentOn);
    }

    [Fact]
    public void MapToEntity_PreservesAllScalarProperties()
    {
        // Arrange
        var sendOn = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
        var domainItem = new Domain.Models.ScheduledItem
        {
            Id = 55,
            ItemType = ScheduledItemType.YouTubeSources,
            ItemPrimaryKey = 33,
            Message = "Entity round-trip",
            SendOnDateTime = sendOn,
            MessageSent = false,
            MessageSentOn = null
        };

        // Act
        var efItem = _mapper.Map<Models.ScheduledItem>(domainItem);

        // Assert
        Assert.Equal(55, efItem.Id);
        Assert.Equal("YouTubeSources", efItem.ItemTableName);
        Assert.Equal(33, efItem.ItemPrimaryKey);
        Assert.Equal("Entity round-trip", efItem.Message);
        Assert.Equal(sendOn, efItem.SendOnDateTime);
        Assert.False(efItem.MessageSent);
        Assert.Null(efItem.MessageSentOn);
    }
}
