using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class ScheduledItemDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly ScheduledItemDataStore _dataStore;

    public ScheduledItemDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new BroadcastingContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var mapper = config.CreateMapper();

        _dataStore = new ScheduledItemDataStore(_context, mapper, NullLogger<ScheduledItemDataStore>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private ScheduledItem CreateScheduledItem(
        string message = "Test Message",
        DateTimeOffset? sendOn = null,
        bool messageSent = false) => new ScheduledItem
    {
        ItemTableName = "Engagements",
        ItemPrimaryKey = 1,
        Message = message,
        SendOnDateTime = sendOn ?? DateTimeOffset.UtcNow.AddHours(1),
        MessageSent = messageSent
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsScheduledItem()
    {
        // Arrange
        var item = CreateScheduledItem("Hello World");
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(item.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(item.Id, result.Id);
        Assert.Equal("Hello World", result.Message);
    }

    [Fact]
    public async Task GetAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllScheduledItems()
    {
        // Arrange
        _context.ScheduledItems.AddRange(
            CreateScheduledItem("Msg 1"),
            CreateScheduledItem("Msg 2"),
            CreateScheduledItem("Msg 3")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_NewScheduledItem_SavesAndReturnsWithId()
    {
        // Arrange
        var sendOn = DateTimeOffset.UtcNow.AddHours(2);
        var domainItem = new Domain.Models.ScheduledItem
        {
            Id = 0,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 10,
            Message = "New Message",
            SendOnDateTime = sendOn
        };

        // Act
        var result = await _dataStore.SaveAsync(domainItem);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("New Message", result.Value!.Message);
    }

    [Fact]
    public async Task SaveAsync_ExistingScheduledItem_UpdatesAndReturns()
    {
        // Arrange
        var item = CreateScheduledItem("Original Message");
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();
        _context.Entry(item).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainItem = new Domain.Models.ScheduledItem
        {
            Id = item.Id,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = item.ItemPrimaryKey,
            Message = "Updated Message",
            SendOnDateTime = item.SendOnDateTime
        };

        // Act
        var result = await _dataStore.SaveAsync(domainItem);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Message", result.Value!.Message);
    }

    [Fact]
    public async Task DeleteAsync_WithScheduledItemObject_DeletesItem()
    {
        // Arrange
        var item = CreateScheduledItem();
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();

        var domainItem = new Domain.Models.ScheduledItem { Id = item.Id };

        // Act
        var result = await _dataStore.DeleteAsync(domainItem);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.ScheduledItems.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesItem()
    {
        // Arrange
        var item = CreateScheduledItem();
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(item.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.ScheduledItems.ToList());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsFailure()
    {
        // Act
        var result = await _dataStore.DeleteAsync(999);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetScheduledItemsToSendAsync_ReturnsItemsPastDueAndUnsent()
    {
        // Arrange
        var past = DateTimeOffset.Now.AddHours(-1);
        var future = DateTimeOffset.Now.AddHours(1);
        _context.ScheduledItems.AddRange(
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 1, Message = "Past Unsent", SendOnDateTime = past, MessageSent = false },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 2, Message = "Past Sent", SendOnDateTime = past, MessageSent = true },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 3, Message = "Future Unsent", SendOnDateTime = future, MessageSent = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetScheduledItemsToSendAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Past Unsent", result[0].Message);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_ReturnsAllUnsentItems()
    {
        // Arrange
        var past = DateTimeOffset.Now.AddHours(-1);
        var future = DateTimeOffset.Now.AddHours(1);
        _context.ScheduledItems.AddRange(
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 1, Message = "Past Unsent", SendOnDateTime = past, MessageSent = false },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 2, Message = "Past Sent", SendOnDateTime = past, MessageSent = true },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 3, Message = "Future Unsent", SendOnDateTime = future, MessageSent = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetUnsentScheduledItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.False(r.MessageSent));
    }

    [Fact]
    public async Task GetScheduledItemsByCalendarMonthAsync_ReturnsItemsInSpecifiedMonth()
    {
        // Arrange
        _context.ScheduledItems.AddRange(
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 1, Message = "June Item", SendOnDateTime = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero), MessageSent = false },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 2, Message = "July Item", SendOnDateTime = new DateTimeOffset(2025, 7, 5, 10, 0, 0, TimeSpan.Zero), MessageSent = false },
            new ScheduledItem { ItemTableName = "Engagements", ItemPrimaryKey = 3, Message = "June Item 2", SendOnDateTime = new DateTimeOffset(2025, 6, 20, 10, 0, 0, TimeSpan.Zero), MessageSent = false }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetScheduledItemsByCalendarMonthAsync(2025, 6);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Contains("June", r.Message));
    }

    [Fact]
    public async Task SentScheduledItemAsync_ExistingId_MarksAsSentAndReturnsTrue()
    {
        // Arrange
        var item = CreateScheduledItem(messageSent: false);
        _context.ScheduledItems.Add(item);
        await _context.SaveChangesAsync();

        var sentOn = DateTimeOffset.UtcNow;

        // Act
        var result = await _dataStore.SentScheduledItemAsync(item.Id, sentOn);

        // Assert
        Assert.True(result);
        var updated = await _context.ScheduledItems.FindAsync(item.Id);
        Assert.NotNull(updated);
        Assert.True(updated.MessageSent);
        Assert.Equal(sentOn, updated.MessageSentOn);
    }

    [Fact]
    public async Task SentScheduledItemAsync_NonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _dataStore.SentScheduledItemAsync(999, DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }
}
