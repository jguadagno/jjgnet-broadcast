using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

/// <summary>
/// Tests for UserEventPublisherMappingDataStore.
/// </summary>
public class UserEventPublisherMappingDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly Mock<ILogger<UserEventPublisherMappingDataStore>> _logger = new();
    private readonly UserEventPublisherMappingDataStore _dataStore;

    public UserEventPublisherMappingDataStoreTests()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new BroadcastingContext(options);

        var mapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.UserPublisherSettingsMappingProfile>();
        }, new LoggerFactory());

        _dataStore = new UserEventPublisherMappingDataStore(
            _context,
            mapperConfiguration.CreateMapper(),
            _logger.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task CreateMappingAsync(
        string ownerOid,
        string eventType,
        int platformId,
        bool isActive = true)
    {
        _context.UserEventPublisherMappings.Add(new UserEventPublisherMapping
        {
            CreatedByEntraOid = ownerOid,
            EventType = eventType,
            SocialMediaPlatformId = platformId,
            IsActive = isActive,
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetByUserAsync_ReturnsOnlyMappingsForThatUser()
    {
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";

        await CreateMappingAsync(ownerAOid, Domain.Constants.MessageTemplates.MessageTypes.NewSyndicationFeedItem, 1);
        await CreateMappingAsync(ownerAOid, Domain.Constants.MessageTemplates.MessageTypes.NewYouTubeItem, 2);
        await CreateMappingAsync(ownerBOid, Domain.Constants.MessageTemplates.MessageTypes.NewSpeakingEngagement, 3);

        var result = await _dataStore.GetByUserAsync(ownerAOid);

        Assert.Equal(2, result.Count);
        Assert.All(result, mapping => Assert.Equal(ownerAOid, mapping.CreatedByEntraOid));
    }

    [Fact]
    public async Task GetByUserAndEventTypeAsync_ReturnsOnlyActiveMappingsForRequestedEvent()
    {
        const string ownerOid = "user-a-oid-11111111";

        await CreateMappingAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewSyndicationFeedItem, 1, isActive: true);
        await CreateMappingAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewSyndicationFeedItem, 2, isActive: false);
        await CreateMappingAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewYouTubeItem, 3, isActive: true);

        var result = await _dataStore.GetByUserAndEventTypeAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewSyndicationFeedItem);

        Assert.Single(result);
        Assert.Equal(1, result[0].SocialMediaPlatformId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappingById()
    {
        const string ownerOid = "user-a-oid-11111111";
        await CreateMappingAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewSpeakingEngagement, 4);
        var entity = await _context.UserEventPublisherMappings.FirstAsync(s => s.CreatedByEntraOid == ownerOid);

        var result = await _dataStore.GetByIdAsync(entity.Id);

        Assert.NotNull(result);
        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(4, result.SocialMediaPlatformId);
    }

    [Fact]
    public async Task SaveAsync_CreatesNewMapping()
    {
        const string ownerOid = "user-a-oid-11111111";
        var mapping = new Domain.Models.UserEventPublisherMapping
        {
            CreatedByEntraOid = ownerOid,
            EventType = Domain.Constants.MessageTemplates.MessageTypes.NewYouTubeItem,
            SocialMediaPlatformId = 2,
            IsActive = true
        };

        var result = await _dataStore.SaveAsync(mapping);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);

        var persisted = await _context.UserEventPublisherMappings.FirstAsync(m => m.Id == result.Id);
        Assert.Equal(ownerOid, persisted.CreatedByEntraOid);
    }

    [Fact]
    public async Task SaveAsync_UpdatesExistingMappingById()
    {
        const string ownerOid = "user-a-oid-11111111";
        await CreateMappingAsync(ownerOid, Domain.Constants.MessageTemplates.MessageTypes.NewYouTubeItem, 2, isActive: true);
        var existing = await _context.UserEventPublisherMappings.FirstAsync(m => m.CreatedByEntraOid == ownerOid);

        var mapping = new Domain.Models.UserEventPublisherMapping
        {
            Id = existing.Id,
            CreatedByEntraOid = ownerOid,
            EventType = Domain.Constants.MessageTemplates.MessageTypes.RandomPost,
            SocialMediaPlatformId = 5,
            IsActive = false
        };

        var result = await _dataStore.SaveAsync(mapping);

        Assert.NotNull(result);
        Assert.Equal(Domain.Constants.MessageTemplates.MessageTypes.RandomPost, result.EventType);
        Assert.Equal(5, result.SocialMediaPlatformId);
        Assert.False(result.IsActive);

        var count = await _context.UserEventPublisherMappings.CountAsync(m => m.CreatedByEntraOid == ownerOid);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseWhenOwnerMismatch()
    {
        const string ownerAOid = "user-a-oid-11111111";
        const string ownerBOid = "user-b-oid-22222222";
        await CreateMappingAsync(ownerAOid, Domain.Constants.MessageTemplates.MessageTypes.NewSyndicationFeedItem, 1);
        var entity = await _context.UserEventPublisherMappings.FirstAsync(m => m.CreatedByEntraOid == ownerAOid);

        var result = await _dataStore.DeleteAsync(entity.Id, ownerBOid);

        Assert.False(result);
        Assert.NotNull(await _context.UserEventPublisherMappings.FindAsync(entity.Id));
    }
}
