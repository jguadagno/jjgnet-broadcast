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
}
