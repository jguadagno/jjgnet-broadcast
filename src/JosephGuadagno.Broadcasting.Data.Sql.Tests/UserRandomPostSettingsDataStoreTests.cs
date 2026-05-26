using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserRandomPostSettingsDataStore.
/// </summary>
public class UserRandomPostSettingsDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserRandomPostSettingsDataStore>> _logger = new();
    private readonly UserRandomPostSettingsDataStore _dataStore;

    public UserRandomPostSettingsDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPublisherSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserRandomPostSettingsDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateSettingsAsync(
        string ownerOid,
        int platformId,
        string cronExpression,
        bool isActive = true,
        DateTimeOffset? cutoffDate = null,
        string? excludedCategories = null)
    {
        _context.UserRandomPostSettings.Add(new UserRandomPostSettings
        {
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = platformId,
            CronExpression = cronExpression,
            CutoffDate = cutoffDate,
            ExcludedCategories = excludedCategories,
            IsActive = isActive,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlySettingsForThatUser()
    {
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";

        await CreateSettingsAsync(ownerAOid, 1, "0 * * * *");
        await CreateSettingsAsync(ownerAOid, 2, "15 * * * *");
        await CreateSettingsAsync(ownerBOid, 3, "30 * * * *");

        var result = await _dataStore.GetByUserAsync(ownerAOid);

        Assert.Equal(2, result.Count);
        Assert.All(result, settings => Assert.Equal(ownerAOid, settings.CreatedByEntraOid));
    }

    [Fact]
    public async Task GetByUserAsync_CanFilterActiveOnly()
    {
        const string ownerOid = "user-a-oid-11111111";

        await CreateSettingsAsync(ownerOid, 1, "0 * * * *", isActive: true);
        await CreateSettingsAsync(ownerOid, 2, "15 * * * *", isActive: false);

        var result = await _dataStore.GetByUserAsync(ownerOid, activeOnly: true);

        Assert.Single(result);
        Assert.True(result[0].IsActive);
        Assert.Equal(1, result[0].SocialMediaPlatformId);
    }

    [Fact]
    public async Task GetAllActiveAsync_ReturnsAllUsersActiveSettings()
    {
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";

        await CreateSettingsAsync(ownerAOid, 1, "0 * * * *", isActive: true);
        await CreateSettingsAsync(ownerAOid, 2, "15 * * * *", isActive: false);
        await CreateSettingsAsync(ownerBOid, 3, "30 * * * *", isActive: true);

        var result = await _dataStore.GetAllActiveAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, settings => Assert.True(settings.IsActive));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedExcludedCategories()
    {
        const string ownerOid = "user-a-oid-11111111";
        await CreateSettingsAsync(ownerOid, 2, "0 * * * *", excludedCategories: "Books,Archive");

        var entity = await _context.UserRandomPostSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        var result = await _dataStore.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(["Books", "Archive"], result.ExcludedCategories);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewSettings()
    {
        const string ownerOid = "user-a-oid-11111111";
        var settings = new Domain.Models.UserRandomPostSettings
        {
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = 2,
            CronExpression = "0 * * * *",
            CutoffDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ExcludedCategories = ["Books", "Archive"],
            IsActive = true
        };

        var result = await _dataStore.SaveAsync(settings);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(["Books", "Archive"], result.ExcludedCategories);

        var persisted = await _context.UserRandomPostSettings.FirstAsync(s => s.Id == result.Id);
        Assert.Equal("Books,Archive", persisted.ExcludedCategories);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettingsById()
    {
        const string ownerOid = "user-a-oid-11111111";
        await CreateSettingsAsync(ownerOid, 2, "0 * * * *", isActive: true, excludedCategories: "Books");
        var existing = await _context.UserRandomPostSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        var settings = new Domain.Models.UserRandomPostSettings
        {
            Id = existing.Id,
            CreatedByEntraOid = ownerOid,
            SocialMediaPlatformId = 3,
            CronExpression = "15 * * * *",
            CutoffDate = null,
            ExcludedCategories = ["News"],
            IsActive = false
        };

        var result = await _dataStore.SaveAsync(settings);

        Assert.NotNull(result);
        Assert.Equal(3, result.SocialMediaPlatformId);
        Assert.Equal("15 * * * *", result.CronExpression);
        Assert.False(result.IsActive);

        var count = await _context.UserRandomPostSettings.CountAsync(s => s.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateSettingsAsync(ownerAOid, 1, "0 * * * *");

        var entity = await _context.UserRandomPostSettings.FirstAsync(s => s.CreatedByEntraOid == ownerAOid);

        var result = await _dataStore.DeleteAsync(entity.Id, ownerBOid);

        Assert.False(result);
        Assert.NotNull(await _context.UserRandomPostSettings.FindAsync(entity.Id));
    }
}
