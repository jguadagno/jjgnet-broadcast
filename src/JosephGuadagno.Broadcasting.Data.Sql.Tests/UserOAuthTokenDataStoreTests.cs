using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserOAuthTokenDataStore
/// </summary>
public class UserOAuthTokenDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserOAuthTokenDataStore>> _logger = new();
    private readonly UserOAuthTokenDataStore _dataStore;

    public UserOAuthTokenDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserOAuthTokenMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserOAuthTokenDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<int> CreatePlatformAsync(string name = "LinkedIn")
    {
        var platform = new SocialMediaPlatform
        {
            Name = name,
            Url = $"https://{name.ToLowerInvariant()}.com",
            Icon = $"bi-{name.ToLowerInvariant()}",
            IsActive = true
        };

        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        return platform.Id;
    }

    private async Task CreateTokenAsync(string ownerOid, int platformId, string accessToken = "token123")
    {
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = platformId,
            AccessToken = accessToken,
            RefreshToken = "refresh123",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTimeOffset.UtcNow.AddDays(60),
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAndPlatformAsync_ReturnsOnlyMatchingUserToken()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerA = "owner-a-oid";
        const string ownerB = "owner-b-oid";

        await CreateTokenAsync(ownerA, platformId, "token-a");
        await CreateTokenAsync(ownerB, platformId, "token-b");

        // Act
        var resultA = await _dataStore.GetByUserAndPlatformAsync(ownerA, platformId);
        var resultB = await _dataStore.GetByUserAndPlatformAsync(ownerB, platformId);

        // Assert
        Assert.NotNull(resultA);
        Assert.Equal(ownerA, resultA.CreatedByEntraOid);
        Assert.Equal("token-a", resultA.AccessToken);

        Assert.NotNull(resultB);
        Assert.Equal(ownerB, resultB.CreatedByEntraOid);
        Assert.Equal("token-b", resultB.AccessToken);
    }

    [Fact]
    public async Task GetByUserAndPlatformAsync_DoesNotReturnOtherUsersTokens()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerA = "owner-a-oid";
        const string ownerB = "owner-b-oid";

        // Only create token for owner B
        await CreateTokenAsync(ownerB, platformId, "token-b");

        // Act - try to get token for owner A
        var resultA = await _dataStore.GetByUserAndPlatformAsync(ownerA, platformId);

        // Assert - should return null, not owner B's token
        Assert.Null(resultA);
    }

    [Fact]
    public async Task UpsertAsync_CreatesTokenForDifferentUsersOnSamePlatform()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerA = "owner-a-oid";
        const string ownerB = "owner-b-oid";

        var tokenA = new Domain.Models.UserOAuthToken
        {
            CreatedByEntraOid = ownerA,
            SocialMediaPlatformId = platformId,
            AccessToken = "token-a",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        var tokenB = new Domain.Models.UserOAuthToken
        {
            CreatedByEntraOid = ownerB,
            SocialMediaPlatformId = platformId,
            AccessToken = "token-b",
            AccessTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        // Act
        await _dataStore.UpsertAsync(tokenA);
        await _dataStore.UpsertAsync(tokenB);

        // Assert - both tokens should exist independently
        var resultA = await _dataStore.GetByUserAndPlatformAsync(ownerA, platformId);
        var resultB = await _dataStore.GetByUserAndPlatformAsync(ownerB, platformId);

        Assert.NotNull(resultA);
        Assert.Equal("token-a", resultA.AccessToken);

        Assert.NotNull(resultB);
        Assert.Equal("token-b", resultB.AccessToken);
    }

    [Fact]
    public async Task DeleteAsync_OnlyDeletesSpecifiedUserToken()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerA = "owner-a-oid";
        const string ownerB = "owner-b-oid";

        await CreateTokenAsync(ownerA, platformId, "token-a");
        await CreateTokenAsync(ownerB, platformId, "token-b");

        // Act - delete owner A's token
        var deleted = await _dataStore.DeleteAsync(ownerA, platformId);

        // Assert
        Assert.True(deleted);

        var resultA = await _dataStore.GetByUserAndPlatformAsync(ownerA, platformId);
        var resultB = await _dataStore.GetByUserAndPlatformAsync(ownerB, platformId);

        Assert.Null(resultA); // Owner A's token should be deleted
        Assert.NotNull(resultB); // Owner B's token should still exist
        Assert.Equal("token-b", resultB.AccessToken);
    }

    [Fact]
    public async Task GetExpiringWindowAsync_ReturnsOnlyTokensWithinWindow()
    {
        // Arrange
        var platformIdA = await CreatePlatformAsync("LinkedIn");
        var platformIdB = await CreatePlatformAsync("Twitter");
        var platformIdC = await CreatePlatformAsync("Facebook");

        var now = DateTimeOffset.UtcNow;
        var windowFrom = now.AddDays(5);
        var windowTo = now.AddDays(10);

        // Token expiring before the window
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformIdA,
            AccessToken = "token-before-window",
            AccessTokenExpiresAt = now.AddDays(2),
            CreatedOn = now,
            LastUpdatedOn = now
        });

        // Token expiring at the start of the window (inclusive)
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-b",
            SocialMediaPlatformId = platformIdA,
            AccessToken = "token-at-window-start",
            AccessTokenExpiresAt = windowFrom,
            CreatedOn = now,
            LastUpdatedOn = now
        });

        // Token expiring within the window
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformIdB,
            AccessToken = "token-inside-window",
            AccessTokenExpiresAt = now.AddDays(7),
            CreatedOn = now,
            LastUpdatedOn = now
        });

        // Token expiring at the end of the window (inclusive)
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformIdC,
            AccessToken = "token-at-window-end",
            AccessTokenExpiresAt = windowTo,
            CreatedOn = now,
            LastUpdatedOn = now
        });

        // Token expiring after the window
        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-b",
            SocialMediaPlatformId = platformIdB,
            AccessToken = "token-after-window",
            AccessTokenExpiresAt = now.AddDays(15),
            CreatedOn = now,
            LastUpdatedOn = now
        });

        await _context.SaveChangesAsync();

        // Act
        var results = await _dataStore.GetExpiringWindowAsync(windowFrom, windowTo);

        // Assert - only tokens within [windowFrom, windowTo] should be returned
        Assert.Equal(3, results.Count);
        Assert.Contains(results, t => t.AccessToken == "token-at-window-start");
        Assert.Contains(results, t => t.AccessToken == "token-inside-window");
        Assert.Contains(results, t => t.AccessToken == "token-at-window-end");
        Assert.DoesNotContain(results, t => t.AccessToken == "token-before-window");
        Assert.DoesNotContain(results, t => t.AccessToken == "token-after-window");
    }

    [Fact]
    public async Task GetExpiringWindowAsync_ReturnsEmptyListWhenNoTokensInWindow()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        var now = DateTimeOffset.UtcNow;

        _context.UserOAuthTokens.Add(new UserOAuthToken
        {
            CreatedByEntraOid = "owner-a",
            SocialMediaPlatformId = platformId,
            AccessToken = "token-outside-window",
            AccessTokenExpiresAt = now.AddDays(30),
            CreatedOn = now,
            LastUpdatedOn = now
        });

        await _context.SaveChangesAsync();

        // Act
        var results = await _dataStore.GetExpiringWindowAsync(now.AddDays(5), now.AddDays(10));

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task UpdateLastNotifiedAtAsync_SetsLastNotifiedAt()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerOid = "owner-a-oid";
        await CreateTokenAsync(ownerOid, platformId);

        var notifiedAt = DateTimeOffset.UtcNow;

        // Act
        var result = await _dataStore.UpdateLastNotifiedAtAsync(ownerOid, platformId, notifiedAt);

        // Assert
        Assert.True(result);

        var token = await _dataStore.GetByUserAndPlatformAsync(ownerOid, platformId);
        Assert.NotNull(token);
        Assert.NotNull(token.LastNotifiedAt);
        Assert.Equal(notifiedAt, token.LastNotifiedAt!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task UpdateLastNotifiedAtAsync_ReturnsFalseWhenTokenNotFound()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");

        // Act - no token exists for this owner
        var result = await _dataStore.UpdateLastNotifiedAtAsync("nonexistent-oid", platformId, DateTimeOffset.UtcNow);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task UpdateLastNotifiedAtAsync_DoesNotAffectOtherUsersTokens()
    {
        // Arrange
        var platformId = await CreatePlatformAsync("LinkedIn");
        const string ownerA = "owner-a-oid";
        const string ownerB = "owner-b-oid";

        await CreateTokenAsync(ownerA, platformId, "token-a");
        await CreateTokenAsync(ownerB, platformId, "token-b");

        var notifiedAt = DateTimeOffset.UtcNow;

        // Act - update only owner A
        await _dataStore.UpdateLastNotifiedAtAsync(ownerA, platformId, notifiedAt);

        // Assert - owner B should not be affected
        var tokenB = await _dataStore.GetByUserAndPlatformAsync(ownerB, platformId);
        Assert.NotNull(tokenB);
        Assert.Null(tokenB.LastNotifiedAt);
    }
}
