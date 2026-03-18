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

    private static MessageTemplate CreateMessageTemplate(
        string platform,
        string messageType,
        string template = "{{ title }} - {{ url }}",
        string? description = null) => new()
    {
        Platform = platform,
        MessageType = messageType,
        Template = template,
        Description = description
    };

    [Fact]
    public async Task GetAsync_WhenTemplateExists_ReturnsMatchingTemplate()
    {
        // Arrange
        _context.MessageTemplates.Add(CreateMessageTemplate("Twitter", "RandomPost", "{{ title }} - {{ url }}", "Twitter random post"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(MessageTemplates.Platforms.Twitter, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Twitter", result.Platform);
        Assert.Equal("RandomPost", result.MessageType);
        Assert.Equal("{{ title }} - {{ url }}", result.Template);
        Assert.Equal("Twitter random post", result.Description);
    }

    [Fact]
    public async Task GetAsync_WhenNoTemplatesExist_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetAsync(MessageTemplates.Platforms.Twitter, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenDifferentPlatformExists_ReturnsNull()
    {
        // Arrange
        _context.MessageTemplates.Add(CreateMessageTemplate("Facebook", "RandomPost"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(MessageTemplates.Platforms.Twitter, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenDifferentMessageTypeExists_ReturnsNull()
    {
        // Arrange
        _context.MessageTemplates.Add(CreateMessageTemplate("Twitter", "NewPost"));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(MessageTemplates.Platforms.Twitter, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenMultiplePlatformsExist_ReturnsCorrectOne()
    {
        // Arrange
        _context.MessageTemplates.AddRange(
            CreateMessageTemplate("Twitter", "RandomPost", "twitter template"),
            CreateMessageTemplate("Facebook", "RandomPost", "facebook template"),
            CreateMessageTemplate("LinkedIn", "RandomPost", "linkedin template"),
            CreateMessageTemplate("Bluesky", "RandomPost", "bluesky template")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(MessageTemplates.Platforms.LinkedIn, MessageTemplates.MessageTypes.RandomPost);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("LinkedIn", result.Platform);
        Assert.Equal("linkedin template", result.Template);
    }

    [Fact]
    public async Task GetAllAsync_WhenMultipleTemplatesExist_ReturnsAll()
    {
        // Arrange
        _context.MessageTemplates.AddRange(
            CreateMessageTemplate("Twitter", "RandomPost"),
            CreateMessageTemplate("Facebook", "RandomPost"),
            CreateMessageTemplate("LinkedIn", "RandomPost"),
            CreateMessageTemplate("Bluesky", "RandomPost")
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
