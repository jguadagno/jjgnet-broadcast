using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserPublisherLinkedInSettingsDataStore — isolation and owner enforcement
/// </summary>
public class UserPublisherLinkedInSettingsDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserPublisherLinkedInSettingsDataStore>> _logger = new();
    private readonly UserPublisherLinkedInSettingsDataStore _dataStore;

    public UserPublisherLinkedInSettingsDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPublisherSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserPublisherLinkedInSettingsDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateLinkedInSettingsAsync(
        string ownerOid,
        string? authorId = "urn:li:person:abc123",
        string? clientId = "client-abc",
        bool isEnabled = true)
    {
        _context.UserPublisherLinkedInSettings.Add(new UserPublisherLinkedInSettings
        {
            CreatedByEntraOid = ownerOid,
            AuthorId = authorId,
            ClientId = clientId,
            IsEnabled = isEnabled,
            HasClientSecret = false,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsSettingsForThatUser()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateLinkedInSettingsAsync(ownerOid, "urn:li:person:abc123", "client-abc");

        // Act
        var result = await _dataStore.GetByUserAsync(ownerOid);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("urn:li:person:abc123", result.AuthorId);
        Assert.Equal("client-abc", result.ClientId);
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsNullWhenUserHasNoSettings()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateLinkedInSettingsAsync(ownerAOid);

        // Act
        var result = await _dataStore.GetByUserAsync(ownerBOid);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsSettingsById()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateLinkedInSettingsAsync(ownerOid);

        var entity = await _context.UserPublisherLinkedInSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        // Act
        var result = await _dataStore.GetByIdAsync(entity.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNullForMissingId()
    {
        // Act
        var result = await _dataStore.GetByIdAsync(99999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewSettings()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        var newSettings = new Domain.Models.UserPublisherLinkedInSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            AuthorId = "urn:li:person:newperson",
            ClientId = "new-client-id",
            HasClientSecret = true
        };

        // Act
        var result = await _dataStore.SaveAsync(newSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(ownerOid, result.CreatedByEntraOid);
        Assert.Equal("urn:li:person:newperson", result.AuthorId);
        Assert.True(result.HasClientSecret);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingSettingsForSameOwner()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateLinkedInSettingsAsync(ownerOid, isEnabled: false);

        var updatedSettings = new Domain.Models.UserPublisherLinkedInSettings
        {
            CreatedByEntraOid = ownerOid,
            IsEnabled = true,
            AuthorId = "urn:li:person:updated",
            ClientId = "updated-client",
            HasClientSecret = true
        };

        // Act
        var result = await _dataStore.SaveAsync(updatedSettings);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsEnabled);
        Assert.Equal("urn:li:person:updated", result.AuthorId);

        var count = await _context.UserPublisherLinkedInSettings
            .CountAsync(s => s.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_DeletesWhenOwnerMatches()
    {
        // Arrange
        const string ownerOid = "user-a-oid-11111111";
        await CreateLinkedInSettingsAsync(ownerOid);

        var entity = await _context.UserPublisherLinkedInSettings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);
        var settingsId = entity.Id;

        // Act
        var result = await _dataStore.DeleteAsync(settingsId, ownerOid);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.UserPublisherLinkedInSettings.FindAsync(settingsId));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        // Arrange
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateLinkedInSettingsAsync(ownerAOid);

        var entity = await _context.UserPublisherLinkedInSettings.FirstAsync(s => s.CreatedByEntraOid == ownerAOid);
        var settingsId = entity.Id;

        // Act — user B attempts to delete user A's settings (MUST FAIL)
        var result = await _dataStore.DeleteAsync(settingsId, ownerBOid);

        // Assert
        Assert.False(result);
        var stillExists = await _context.UserPublisherLinkedInSettings.FindAsync(settingsId);
        Assert.NotNull(stillExists);
        Assert.Equal(ownerAOid, stillExists.CreatedByEntraOid);
    }
}
