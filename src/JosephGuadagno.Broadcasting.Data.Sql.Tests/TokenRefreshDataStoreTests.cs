using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class TokenRefreshDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly TokenRefreshDataStore _dataStore;

    public TokenRefreshDataStoreTests()
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

        _dataStore = new TokenRefreshDataStore(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private TokenRefresh CreateTokenRefresh(string name = "TestToken") => new TokenRefresh
    {
        Name = name,
        Expires = DateTimeOffset.UtcNow.AddDays(30),
        LastChecked = DateTimeOffset.UtcNow,
        LastRefreshed = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsTokenRefresh()
    {
        // Arrange
        var tokenRefresh = CreateTokenRefresh();
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(tokenRefresh.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tokenRefresh.Id, result.Id);
        Assert.Equal("TestToken", result.Name);
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
    public async Task GetAllAsync_ReturnsAllTokenRefreshes()
    {
        // Arrange
        _context.TokenRefreshes.AddRange(
            CreateTokenRefresh("Token1"),
            CreateTokenRefresh("Token2"),
            CreateTokenRefresh("Token3")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_NewTokenRefresh_SavesAndReturnsWithId()
    {
        // Arrange
        var domainToken = new Domain.Models.TokenRefresh
        {
            Id = 0,
            Name = "NewToken",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            LastChecked = DateTimeOffset.UtcNow,
            LastRefreshed = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _dataStore.SaveAsync(domainToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("NewToken", result.Value!.Name);
    }

    [Fact]
    public async Task SaveAsync_ExistingTokenRefresh_UpdatesAndReturns()
    {
        // Arrange
        var tokenRefresh = CreateTokenRefresh("OriginalToken");
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();
        _context.Entry(tokenRefresh).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainToken = new Domain.Models.TokenRefresh
        {
            Id = tokenRefresh.Id,
            Name = "UpdatedToken",
            Expires = DateTimeOffset.UtcNow.AddDays(60),
            LastChecked = DateTimeOffset.UtcNow,
            LastRefreshed = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

        // Act
        var result = await _dataStore.SaveAsync(domainToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("UpdatedToken", result.Value!.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithTokenRefreshObject_DeletesTokenRefresh()
    {
        // Arrange
        var tokenRefresh = CreateTokenRefresh();
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();

        var domainToken = new Domain.Models.TokenRefresh { Id = tokenRefresh.Id };

        // Act
        var result = await _dataStore.DeleteAsync(domainToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.TokenRefreshes.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesTokenRefresh()
    {
        // Arrange
        var tokenRefresh = CreateTokenRefresh();
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(tokenRefresh.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.TokenRefreshes.ToList());
    }

    [Fact]
    public async Task DeleteAsync_NonExistingId_ReturnsTrue()
    {
        // Act
        var result = await _dataStore.DeleteAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsTokenRefresh()
    {
        // Arrange
        var tokenRefresh = CreateTokenRefresh("MyToken");
        _context.TokenRefreshes.Add(tokenRefresh);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync("MyToken");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MyToken", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_NonExistingName_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }
}
