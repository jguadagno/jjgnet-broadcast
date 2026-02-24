using JosephGuadagno.Broadcasting.Data;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Data.Tests;

public class DataRepositoryBaseTests
{
    private readonly Mock<IDataStore<ScheduledItem>> _dataStoreMock;
    private readonly DataRepositoryBase<ScheduledItem> _repository;

    public DataRepositoryBaseTests()
    {
        _dataStoreMock = new Mock<IDataStore<ScheduledItem>>();
        _repository = new DataRepositoryBase<ScheduledItem>(_dataStoreMock.Object);
    }

    [Fact]
    public async Task GetAsync_WithPrimaryKey_CallsDataStore()
    {
        // Arrange
        var expected = new ScheduledItem { Id = 1, ItemTableName = "SourceData", ItemPrimaryKey = 42, Message = "Test", SendOnDateTime = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.GetAsync(1)).ReturnsAsync(expected);

        // Act
        var result = await _repository.GetAsync(1);

        // Assert
        Assert.Equal(expected, result);
        _dataStoreMock.Verify(d => d.GetAsync(1), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_WithEntity_CallsDataStore()
    {
        // Arrange
        var entity = new ScheduledItem { Id = 1, ItemTableName = "SourceData", ItemPrimaryKey = 42, Message = "Test", SendOnDateTime = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.SaveAsync(entity)).ReturnsAsync(entity);

        // Act
        var result = await _repository.SaveAsync(entity);

        // Assert
        Assert.Equal(entity, result);
        _dataStoreMock.Verify(d => d.SaveAsync(entity), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllItems()
    {
        // Arrange
        var items = new List<ScheduledItem>
        {
            new ScheduledItem { Id = 1, ItemTableName = "SourceData", ItemPrimaryKey = 1, Message = "Msg1", SendOnDateTime = DateTimeOffset.UtcNow },
            new ScheduledItem { Id = 2, ItemTableName = "SourceData", ItemPrimaryKey = 2, Message = "Msg2", SendOnDateTime = DateTimeOffset.UtcNow }
        };
        _dataStoreMock.Setup(d => d.GetAllAsync()).ReturnsAsync(items);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(items, result);
        _dataStoreMock.Verify(d => d.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_CallsDataStore()
    {
        // Arrange
        var entity = new ScheduledItem { Id = 1, ItemTableName = "SourceData", ItemPrimaryKey = 1, Message = "Msg", SendOnDateTime = DateTimeOffset.UtcNow };
        _dataStoreMock.Setup(d => d.DeleteAsync(entity)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(entity);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(entity), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithPrimaryKey_CallsDataStore()
    {
        // Arrange
        _dataStoreMock.Setup(d => d.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _repository.DeleteAsync(1);

        // Assert
        Assert.True(result);
        _dataStoreMock.Verify(d => d.DeleteAsync(1), Times.Once);
    }
}
