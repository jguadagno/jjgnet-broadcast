using Moq;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class ScheduledItemManagerTests
{
    private readonly Mock<IScheduledItemRepository> _repository;
    private readonly ScheduledItemManager _scheduledItemManager;

    public ScheduledItemManagerTests()
    {
        _repository = new Mock<IScheduledItemRepository>();
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
        _repository.Setup(r => r.SaveAsync(scheduledItem)).ReturnsAsync(scheduledItem);

        // Act
        var result = await _scheduledItemManager.SaveAsync(scheduledItem);

        // Assert
        Assert.Equal(scheduledItem, result);
        _repository.Verify(r => r.SaveAsync(scheduledItem), Times.Once);
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
    public async Task DeleteAsync_Entity_ShouldCallRepository()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        _repository.Setup(r => r.DeleteAsync(scheduledItem)).ReturnsAsync(true);

        // Act
        var result = await _scheduledItemManager.DeleteAsync(scheduledItem);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(scheduledItem), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_PrimaryKey_ShouldCallRepository()
    {
        // Arrange
        _repository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _scheduledItemManager.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _repository.Verify(r => r.DeleteAsync(1), Times.Once);
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
}