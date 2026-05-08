using Moq;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class ScheduledItemManagerTests
{
    private readonly Mock<IScheduledItemDataStore> _repository;
    private readonly ScheduledItemManager _scheduledItemManager;

    public ScheduledItemManagerTests()
    {
        _repository = new Mock<IScheduledItemDataStore>();
        _scheduledItemManager = new ScheduledItemManager(_repository.Object);
    }

    [Fact]
    public async Task GetAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        _repository.Setup(r => r.GetAsync(1)).ReturnsAsync(scheduledItem);

        // Act
        var result = await _scheduledItemManager.GetAsync(1);

        // Assert
        Assert.Equal(scheduledItem, result);
        _repository.Verify(r => r.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        _repository.Setup(r => r.SaveAsync(scheduledItem, default)).ReturnsAsync(OperationResult<ScheduledItem>.Success(scheduledItem));

        // Act
        var result = await _scheduledItemManager.SaveAsync(scheduledItem);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(scheduledItem, result.Value);
        _repository.Verify(r => r.SaveAsync(scheduledItem, default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetAllAsync();

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetAllAsync("owner-1", default)).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetAllAsync("owner-1");

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetAllAsync("owner-1", default), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WithOwnerOidAndPaging_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var pagedResult = new PagedResult<ScheduledItem>
        {
            Items = new List<ScheduledItem> { new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-1" } },
            TotalCount = 1
        };
        _repository.Setup(r => r.GetAllAsync("owner-1", 2, 5, default)).ReturnsAsync(pagedResult);

        // Act
        var result = await _scheduledItemManager.GetAllAsync("owner-1", 2, 5, cancellationToken: default);

        // Assert
        Assert.Equal(pagedResult, result);
        _repository.Verify(r => r.GetAllAsync("owner-1", 2, 5, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(scheduledItem, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _scheduledItemManager.DeleteAsync(scheduledItem);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(scheduledItem, default), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1, default)).ReturnsAsync(OperationResult<bool>.Success(true));

        // Act
        var result = await _scheduledItemManager.DeleteAsync(1);

        // Assert
        Assert.True(result.IsSuccess);
        _repository.Verify(r => r.DeleteAsync(1, default), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsToSendAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        _repository.Setup(r => r.GetScheduledItemsToSendAsync()).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetScheduledItemsToSendAsync();

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetScheduledItemsToSendAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        _repository.Setup(r => r.GetUnsentScheduledItemsAsync()).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetUnsentScheduledItemsAsync();

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetUnsentScheduledItemsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetUnsentScheduledItemsAsync("owner-1", default)).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetUnsentScheduledItemsAsync("owner-1");

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetUnsentScheduledItemsAsync("owner-1", default), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsByCalendarMonthAsync_ShouldCallRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        _repository.Setup(r => r.GetScheduledItemsByCalendarMonthAsync(2022, 1)).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetScheduledItemsByCalendarMonthAsync(2022, 1);

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetScheduledItemsByCalendarMonthAsync(2022, 1), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsByCalendarMonthAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetScheduledItemsByCalendarMonthAsync("owner-1", 2022, 1, default)).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetScheduledItemsByCalendarMonthAsync("owner-1", 2022, 1);

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetScheduledItemsByCalendarMonthAsync("owner-1", 2022, 1, default), Times.Once);
    }

    [Fact]
    public async Task SentScheduledItemAsync_WithIdOnly_ShouldCallRepositoryWithUtcNow()
    {
        // Arrange
        _repository.Setup(r => r.SentScheduledItemAsync(1, It.IsAny<DateTimeOffset>())).ReturnsAsync(true);

        // Act
        var result = await _scheduledItemManager.SentScheduledItemAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.SentScheduledItemAsync(1, It.Is<DateTimeOffset>(d => (DateTimeOffset.UtcNow - d).TotalSeconds < 5)), Times.Once);
    }

    [Fact]
    public async Task SentScheduledItemAsync_WithIdAndDate_ShouldCallRepository()
    {
        // Arrange
        var sentOn = DateTimeOffset.UtcNow;
        _repository.Setup(r => r.SentScheduledItemAsync(1, sentOn)).ReturnsAsync(true);

        // Act
        var result = await _scheduledItemManager.SentScheduledItemAsync(1, sentOn);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.SentScheduledItemAsync(1, sentOn), Times.Once);
    }

    [Fact]
    public async Task GetOrphanedScheduledItemsAsync_WithOwnerOid_ShouldCallOwnerFilteredRepository()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-1" } };
        _repository.Setup(r => r.GetOrphanedScheduledItemsAsync("owner-1", default)).ReturnsAsync(scheduledItems);

        // Act
        var result = await _scheduledItemManager.GetOrphanedScheduledItemsAsync("owner-1");

        // Assert
        Assert.Equal(scheduledItems, result);
        _repository.Verify(r => r.GetOrphanedScheduledItemsAsync("owner-1", default), Times.Once);
    }
}
