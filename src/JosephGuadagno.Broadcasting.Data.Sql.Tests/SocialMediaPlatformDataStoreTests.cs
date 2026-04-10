using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class SocialMediaPlatformDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<SocialMediaPlatformDataStore>> _mockLogger;
    private readonly SocialMediaPlatformDataStore _dataStore;

    public SocialMediaPlatformDataStoreTests()
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

        _mockLogger = new Mock<ILogger<SocialMediaPlatformDataStore>>();
        _dataStore = new SocialMediaPlatformDataStore(_context, mapper, _mockLogger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private SocialMediaPlatform CreateDbPlatform(int id = 0, string name = "Twitter", bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        Url = $"https://{name.ToLower()}.com",
        Icon = $"bi-{name.ToLower()}",
        IsActive = isActive
    };

    private Domain.Models.SocialMediaPlatform CreateDomainPlatform(int id = 0, string name = "Twitter", bool isActive = true) => new()
    {
        Id = id,
        Name = name,
        Url = $"https://{name.ToLower()}.com",
        Icon = $"bi-{name.ToLower()}",
        IsActive = isActive
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsPlatform()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "Twitter");
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("Twitter", result.Name);
        Assert.Equal("https://twitter.com", result.Url);
        Assert.Equal("bi-twitter", result.Icon);
        Assert.True(result.IsActive);
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
    public async Task GetAsync_WithCancellationToken_RespectsToken()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "Twitter");
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        var cts = new CancellationTokenSource();

        // Act
        var result = await _dataStore.GetAsync(1, cts.Token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Twitter", result.Name);
    }

    [Fact]
    public async Task GetAllAsync_WithoutInactive_ReturnsOnlyActivePlatforms()
    {
        // Arrange
        _context.SocialMediaPlatforms.AddRange(
            CreateDbPlatform(1, "Twitter", isActive: true),
            CreateDbPlatform(2, "Facebook", isActive: true),
            CreateDbPlatform(3, "GooglePlus", isActive: false)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync(includeInactive: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.True(p.IsActive));
        Assert.Contains(result, p => p.Name == "Twitter");
        Assert.Contains(result, p => p.Name == "Facebook");
        Assert.DoesNotContain(result, p => p.Name == "GooglePlus");
    }

    [Fact]
    public async Task GetAllAsync_WithInactive_ReturnsAllPlatforms()
    {
        // Arrange
        _context.SocialMediaPlatforms.AddRange(
            CreateDbPlatform(1, "Twitter", isActive: true),
            CreateDbPlatform(2, "Facebook", isActive: true),
            CreateDbPlatform(3, "GooglePlus", isActive: false)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync(includeInactive: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, p => p.Name == "Twitter");
        Assert.Contains(result, p => p.Name == "Facebook");
        Assert.Contains(result, p => p.Name == "GooglePlus");
    }

    [Fact]
    public async Task GetAllAsync_OrdersByName_ReturnsAlphabeticallyOrdered()
    {
        // Arrange
        _context.SocialMediaPlatforms.AddRange(
            CreateDbPlatform(1, "Zello", isActive: true),
            CreateDbPlatform(2, "Apple", isActive: true),
            CreateDbPlatform(3, "Microsoft", isActive: true)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].Name);
        Assert.Equal("Microsoft", result[1].Name);
        Assert.Equal("Zello", result[2].Name);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingName_ReturnsPlatform()
    {
        // Arrange
        _context.SocialMediaPlatforms.AddRange(
            CreateDbPlatform(1, "Twitter", isActive: true),
            CreateDbPlatform(2, "Facebook", isActive: true)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync("Twitter");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Twitter", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive_ReturnsPlatform()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "Twitter", isActive: true);
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync("twitter");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Twitter", result.Name);
    }

    [Fact]
    public async Task GetByNameAsync_InactivePlatform_ReturnsNull()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "GooglePlus", isActive: false);
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync("GooglePlus");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByNameAsync_NonExistingName_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task AddAsync_ValidPlatform_AddsAndReturnsPlatform()
    {
        // Arrange
        var domainPlatform = CreateDomainPlatform(0, "LinkedIn");

        // Act
        var result = await _dataStore.AddAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("LinkedIn", result.Name);
        Assert.Equal("https://linkedin.com", result.Url);
        Assert.Equal("bi-linkedin", result.Icon);
        Assert.True(result.IsActive);

        var dbPlatform = await _context.SocialMediaPlatforms.FirstOrDefaultAsync();
        Assert.NotNull(dbPlatform);
        Assert.Equal("LinkedIn", dbPlatform.Name);
    }

    [Fact]
    public async Task UpdateAsync_ExistingPlatform_UpdatesAndReturnsPlatform()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "Twitter");
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        _context.Entry(platform).State = EntityState.Detached;

        var domainPlatform = CreateDomainPlatform(1, "TwitterX");
        domainPlatform.Url = "https://x.com";
        domainPlatform.Icon = "bi-twitter-x";

        // Act
        var result = await _dataStore.UpdateAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
        Assert.Equal("TwitterX", result.Name);
        Assert.Equal("https://x.com", result.Url);
        Assert.Equal("bi-twitter-x", result.Icon);
    }

    [Fact]
    public async Task DeleteAsync_ExistingPlatform_SoftDeletesAndReturnsTrue()
    {
        // Arrange
        var platform = CreateDbPlatform(1, "GooglePlus", isActive: true);
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(1);

        // Assert
        Assert.True(result);
        
        var dbPlatform = await _context.SocialMediaPlatforms.FindAsync(1);
        Assert.NotNull(dbPlatform);
        Assert.False(dbPlatform.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_NonExistingPlatform_ReturnsFalse()
    {
        // Act
        var result = await _dataStore.DeleteAsync(999);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(100)]
    public async Task GetAsync_VariousIds_ReturnsCorrectPlatform(int id)
    {
        // Arrange
        var platform = CreateDbPlatform(id, $"Platform{id}");
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal($"Platform{id}", result.Name);
    }

    [Theory]
    [InlineData("Twitter")]
    [InlineData("TWITTER")]
    [InlineData("twitter")]
    [InlineData("TwItTeR")]
    public async Task GetByNameAsync_VariousCases_ReturnsPlatform(string searchName)
    {
        // Arrange
        var platform = CreateDbPlatform(1, "Twitter", isActive: true);
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAsync(searchName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Twitter", result.Name);
    }
}
