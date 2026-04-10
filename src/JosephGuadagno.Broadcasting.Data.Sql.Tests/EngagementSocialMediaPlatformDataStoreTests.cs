using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class EngagementSocialMediaPlatformDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly EngagementSocialMediaPlatformDataStore _dataStore;

    public EngagementSocialMediaPlatformDataStoreTests()
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

        _dataStore = new EngagementSocialMediaPlatformDataStore(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<int> CreateEngagementAsync(string name = "Test Engagement")
    {
        var engagement = new Engagement
        {
            Name = name,
            Url = "https://example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC",
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();
        return engagement.Id;
    }

    private async Task<int> CreateSocialMediaPlatformAsync(string name = "Twitter", bool isActive = true)
    {
        var platform = new SocialMediaPlatform
        {
            Name = name,
            IsActive = isActive
        };
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        return platform.Id;
    }

    private static EngagementSocialMediaPlatform CreateDbEngagementSocialMediaPlatform(
        int engagementId,
        int platformId,
        string? handle = null) => new()
    {
        EngagementId = engagementId,
        SocialMediaPlatformId = platformId,
        Handle = handle
    };

    #region GetByEngagementIdAsync Tests

    [Fact]
    public async Task GetByEngagementIdAsync_WhenPlatformsExist_ReturnsList()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platform1Id = await CreateSocialMediaPlatformAsync("Twitter");
        var platform2Id = await CreateSocialMediaPlatformAsync("LinkedIn");
        
        _context.EngagementSocialMediaPlatforms.AddRange(
            CreateDbEngagementSocialMediaPlatform(engagementId, platform1Id, "@testhandle"),
            CreateDbEngagementSocialMediaPlatform(engagementId, platform2Id, "@linkedinhandle")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.SocialMediaPlatformId == platform1Id);
        Assert.Contains(result, p => p.SocialMediaPlatformId == platform2Id);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_WhenNoPlatformsExist_ReturnsEmptyList()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_WhenDifferentEngagementHasPlatforms_ReturnsEmptyList()
    {
        // Arrange
        var engagement1Id = await CreateEngagementAsync("Engagement 1");
        var engagement2Id = await CreateEngagementAsync("Engagement 2");
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagement1Id, platformId)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagement2Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_IncludesSocialMediaPlatformNavigation()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync("Bluesky");
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platformId, "@blueskyhandle")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].SocialMediaPlatform);
        Assert.Equal("Bluesky", result[0].SocialMediaPlatform!.Name);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WhenValid_AddsAndReturnsEntity()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        var domainPlatform = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = "@testhandle"
        };

        // Act
        var result = await _dataStore.AddAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(engagementId, result.EngagementId);
        Assert.Equal(platformId, result.SocialMediaPlatformId);
        Assert.Equal("@testhandle", result.Handle);

        var dbEntity = await _context.EngagementSocialMediaPlatforms
            .FirstOrDefaultAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platformId);
        Assert.NotNull(dbEntity);
    }

    [Fact]
    public async Task AddAsync_WhenHandleIsNull_AddsSuccessfully()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        var domainPlatform = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = null
        };

        // Act
        var result = await _dataStore.AddAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Handle);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platformId, "@deletetest")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platformId);

        // Assert
        Assert.True(result);
        
        var dbEntity = await _context.EngagementSocialMediaPlatforms
            .FirstOrDefaultAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platformId);
        Assert.Null(dbEntity);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platformId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenEngagementIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var engagement1Id = await CreateEngagementAsync("Engagement 1");
        var engagement2Id = await CreateEngagementAsync("Engagement 2");
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagement1Id, platformId)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagement2Id, platformId);

        // Assert
        Assert.False(result);
        
        var stillExists = await _context.EngagementSocialMediaPlatforms
            .AnyAsync(e => e.EngagementId == engagement1Id && e.SocialMediaPlatformId == platformId);
        Assert.True(stillExists);
    }

    [Fact]
    public async Task DeleteAsync_WhenPlatformIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platform1Id = await CreateSocialMediaPlatformAsync("Twitter");
        var platform2Id = await CreateSocialMediaPlatformAsync("LinkedIn");
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platform1Id)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platform2Id);

        // Assert
        Assert.False(result);
        
        var stillExists = await _context.EngagementSocialMediaPlatforms
            .AnyAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platform1Id);
        Assert.True(stillExists);
    }

    #endregion
}
