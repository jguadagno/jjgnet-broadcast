using JosephGuadagno.Broadcasting.Data.Repositories;
using JosephGuadagno.Broadcasting.Domain.Enums;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests.Repositories;

public class ScheduledItemRepositoryTests
{
    private readonly Mock<IScheduledItemDataStore> _dataStoreMock;
    private readonly ScheduledItemRepository _repository;

    public ScheduledItemRepositoryTests()
    {
        _dataStoreMock = new Mock<IScheduledItemDataStore>();
        _repository = new ScheduledItemRepository(_dataStoreMock.Object);
    }

    private static ScheduledItem CreateItem(int id = 1) =>
        new ScheduledItem { Id = id, ItemType = ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = id, Message = "Test message", SendOnDateTime = DateTimeOffset.UtcNow };

    [Fact]
    public async Task GetAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        var item = CreateItem();
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(item);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(item, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var item = CreateItem();
        _dataStoreMock.Setup(d => d.SaveAsync(item)).ReturnsAsync(item);

        // Act
        var result = await _repository.SaveAsync(item);

        // Assert
        Assert.Equal(item, result);
        _dataStoreMock.Verify(d => d.SaveAsync(item), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_DelegatesToDataStore()
    {
        // Arrange
        var items = new List<ScheduledItem> { CreateItem(1), CreateItem(2) };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(items, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_DelegatesToDataStore()
    {
        // Arrange
        var item = CreateItem();
        _dataStoreMock.Setup(d => d.DeleteAsync(item)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(item);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(item), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPrimaryKey_DelegatesToDataStore()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsToSendAsync_DelegatesToDataStore()
    {
        // Arrange
        var items = new List<ScheduledItem> { CreateItem(1) };
        _dataStoreMock.Setup(d => d.GetScheduledItemsToSendAsync()).ReturnsAsync(items);

        // Act
        var result = await _repository.GetScheduledItemsToSendAsync();

        // Assert
        Assert.Equal(items, result);
        _dataStoreMock.Verify(d => d.GetScheduledItemsToSendAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_DelegatesToDataStore()
    {
        // Arrange
        var items = new List<ScheduledItem> { CreateItem(1), CreateItem(2) };
        _dataStoreMock.Setup(d => d.GetUnsentScheduledItemsAsync()).ReturnsAsync(items);

        // Act
        var result = await _repository.GetUnsentScheduledItemsAsync();

        // Assert
        Assert.Equal(items, result);
        _dataStoreMock.Verify(d => d.GetUnsentScheduledItemsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsByCalendarMonthAsync_DelegatesToDataStore()
    {
        // Arrange
        var items = new List<ScheduledItem> { CreateItem(1) };
        _dataStoreMock.Setup(d => d.GetScheduledItemsByCalendarMonthAsync(2025, 6)).ReturnsAsync(items);

        // Act
        var result = await _repository.GetScheduledItemsByCalendarMonthAsync(2025, 6);

        // Assert
        Assert.Equal(items, result);
        _dataStoreMock.Verify(d => d.GetScheduledItemsByCalendarMonthAsync(2025, 6), Times.Once);
    }

    [Fact]
    public async Task SentScheduledItemAsync_DelegatesToDataStore()
    {
        // Arrange
        var sentOn = DateTimeOffset.UtcNow;
        _dataStoreMock.Setup(d => d.SentScheduledItemAsync(1, sentOn)).ReturnsAsync(true);

        // Act
        var result = await _repository.SentScheduledItemAsync(1, sentOn);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.SentScheduledItemAsync(1, sentOn), Times.Once);
    }
}
