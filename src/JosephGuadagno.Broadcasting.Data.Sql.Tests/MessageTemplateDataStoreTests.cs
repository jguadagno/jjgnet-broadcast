using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using JosephGuadagno.Broadcasting.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class MessageTemplateDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly MessageTemplateDataStore _dataStore;

    public MessageTemplateDataStoreTests()
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

        _dataStore = new MessageTemplateDataStore(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private async Task<int> GetOrCreatePlatformIdAsync(string platformName)
    {
        var platform = _context.SocialMediaPlatforms.FirstOrDefault(p => p.Name == platformName);
        if (platform == null)
        {
            platform = new Data.Sql.Models.SocialMediaPlatform
            {
                Name = platformName,
                IsActive = true
            };
            _context.SocialMediaPlatforms.Add(platform);
            await _context.SaveChangesAsync();
        }
        return platform.Id;
    }

    private static Data.Sql.Models.MessageTemplate CreateMessageTemplate(
        int platformId,
        string messageType,
        string template = "{{ title }} - {{ url }}",
        string? description = null) => new()
    {
        SocialMediaPlatformId = platformId,
        MessageType = messageType,
        Template = template,
        Description = description
    };

    [Fact]
    public async Task GetAsync_WhenTemplateExists_ReturnsMatchingTemplate()
    {
        // Arrange
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        _context.MessageTemplates.Add(CreateMessageTemplate(twitterId, "RandomPost", "{{ title }} - {{ url }}", "Twitter random post"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(twitterId, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(twitterId, result.SocialMediaPlatformId);
        Assert.Equal("RandomPost", result.MessageType);
        Assert.Equal("{{ title }} - {{ url }}", result.Template);
        Assert.Equal("Twitter random post", result.Description);
    }

    [Fact]
    public async Task GetAsync_WhenNoTemplatesExist_ReturnsNull()
    {
        // Arrange
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        
        // Act
        var result = await _dataStore.GetAsync(twitterId, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenDifferentPlatformExists_ReturnsNull()
    {
        // Arrange
        var facebookId = await GetOrCreatePlatformIdAsync("Facebook");
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        _context.MessageTemplates.Add(CreateMessageTemplate(facebookId, "RandomPost"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(twitterId, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenDifferentMessageTypeExists_ReturnsNull()
    {
        // Arrange
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        _context.MessageTemplates.Add(CreateMessageTemplate(twitterId, "NewPost"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(twitterId, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenMultiplePlatformsExist_ReturnsCorrectOne()
    {
        // Arrange
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        var facebookId = await GetOrCreatePlatformIdAsync("Facebook");
        var linkedInId = await GetOrCreatePlatformIdAsync("LinkedIn");
        var blueskyId = await GetOrCreatePlatformIdAsync("Bluesky");
        
        _context.MessageTemplates.AddRange(
            CreateMessageTemplate(twitterId, "RandomPost", "twitter template"),
            CreateMessageTemplate(facebookId, "RandomPost", "facebook template"),
            CreateMessageTemplate(linkedInId, "RandomPost", "linkedin template"),
            CreateMessageTemplate(blueskyId, "RandomPost", "bluesky template")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(linkedInId, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(linkedInId, result.SocialMediaPlatformId);
        Assert.Equal("linkedin template", result.Template);
    }

    [Fact]
    public async Task GetAllAsync_WhenMultipleTemplatesExist_ReturnsAll()
    {
        // Arrange
        var twitterId = await GetOrCreatePlatformIdAsync("Twitter");
        var facebookId = await GetOrCreatePlatformIdAsync("Facebook");
        var linkedInId = await GetOrCreatePlatformIdAsync("LinkedIn");
        var blueskyId = await GetOrCreatePlatformIdAsync("Bluesky");
        
        _context.MessageTemplates.AddRange(
            CreateMessageTemplate(twitterId, "RandomPost"),
            CreateMessageTemplate(facebookId, "RandomPost"),
            CreateMessageTemplate(linkedInId, "RandomPost"),
            CreateMessageTemplate(blueskyId, "RandomPost")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoTemplatesExist_ReturnsEmptyList()
    {
        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.Empty(result);
    }
}
