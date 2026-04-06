using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class EngagementDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly EngagementDataStore _dataStore;

    public EngagementDataStoreTests()
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

        _dataStore = new EngagementDataStore(_context, mapper);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private Engagement CreateEngagement(int id = 0, string name = "Test Conference") => new Engagement
    {
        Id = id,
        Name = name,
        Url = "https://example.com",
        StartDateTime = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero),
        EndDateTime = new DateTimeOffset(2025, 6, 3, 17, 0, 0, TimeSpan.Zero),
        TimeZoneId = "UTC",
        CreatedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };

    private Talk CreateTalk(int engagementId = 0, string name = "My Talk") => new Talk
    {
        EngagementId = engagementId,
        Name = name,
        UrlForConferenceTalk = "https://conf.example.com/talk",
        UrlForTalk = "https://example.com/talk",
        StartDateTime = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero),
        EndDateTime = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task GetAsync_ExistingId_ReturnsEngagementWithTalks()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var talk = CreateTalk(engagement.Id);
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(engagement.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(engagement.Id, result.Id);
        Assert.Equal("Test Conference", result.Name);
        Assert.NotNull(result.Talks);
        Assert.Single(result.Talks);
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
    public async Task GetAllAsync_ReturnsAllEngagements()
    {
        // Arrange
        _context.Engagements.AddRange(
            CreateEngagement(name: "Conf A"),
            CreateEngagement(name: "Conf B"),
            CreateEngagement(name: "Conf C")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SaveAsync_NewEngagement_SavesAndReturnsWithId()
    {
        // Arrange
        var domainEngagement = new Domain.Models.Engagement
        {
            Id = 0,
            Name = "New Conference",
            Url = "https://newconf.example.com",
            StartDateTime = new DateTimeOffset(2025, 9, 1, 9, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 9, 3, 17, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };

        // Act
        var result = await _dataStore.SaveAsync(domainEngagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("New Conference", result.Value!.Name);
    }

    [Fact]
    public async Task SaveAsync_ExistingEngagement_UpdatesAndReturns()
    {
        // Arrange
        var engagement = CreateEngagement(name: "Original Name");
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();
        _context.Entry(engagement).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainEngagement = new Domain.Models.Engagement
        {
            Id = engagement.Id,
            Name = "Updated Name",
            Url = "https://example.com",
            StartDateTime = engagement.StartDateTime,
            EndDateTime = engagement.EndDateTime,
            TimeZoneId = "UTC"
        };

        // Act
        var result = await _dataStore.SaveAsync(domainEngagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Name", result.Value!.Name);
    }

    [Fact]
    public async Task DeleteAsync_WithEngagementObject_DeletesEngagementAndTalks()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        _context.Talks.Add(CreateTalk(engagement.Id));
        await _context.SaveChangesAsync();

        var domainEngagement = new Domain.Models.Engagement { Id = engagement.Id };

        // Act
        var result = await _dataStore.DeleteAsync(domainEngagement);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Engagements.ToList());
        Assert.Empty(_context.Talks.ToList());
    }

    [Fact]
    public async Task DeleteAsync_WithId_DeletesEngagementAndTalks()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        _context.Talks.Add(CreateTalk(engagement.Id));
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagement.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Engagements.ToList());
        Assert.Empty(_context.Talks.ToList());
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
    public async Task GetTalksForEngagementAsync_ReturnsCorrectTalks()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        _context.Talks.AddRange(
            CreateTalk(engagement.Id, "Talk 1"),
            CreateTalk(engagement.Id, "Talk 2")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetTalksForEngagementAsync(engagement.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_NullEngagement_ThrowsArgumentNullException()
    {
        // Arrange
        var talk = new Domain.Models.Talk { Name = "My Talk" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _dataStore.AddTalkToEngagementAsync(null!, talk));
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_NullTalk_ThrowsArgumentNullException()
    {
        // Arrange
        var engagement = new Domain.Models.Engagement { Id = 1, Name = "Conf" };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _dataStore.AddTalkToEngagementAsync(engagement, null!));
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_NewEngagement_AddsEngagementAndTalk()
    {
        // Arrange
        var domainEngagement = new Domain.Models.Engagement
        {
            Id = 0,
            Name = "New Conf",
            Url = "https://example.com",
            StartDateTime = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 6, 3, 17, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };

        var domainTalk = new Domain.Models.Talk
        {
            Name = "My Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = await _dataStore.AddTalkToEngagementAsync(domainEngagement, domainTalk);

        // Assert
        Assert.True(result);
        Assert.Single(_context.Engagements.ToList());
        Assert.Single(_context.Talks.ToList());
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_ExistingEngagement_AddsTalk()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var domainEngagement = new Domain.Models.Engagement { Id = engagement.Id, Name = engagement.Name };
        var domainTalk = new Domain.Models.Talk
        {
            Name = "New Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = await _dataStore.AddTalkToEngagementAsync(domainEngagement, domainTalk);

        // Assert
        Assert.True(result);
        Assert.Single(_context.Talks.ToList());
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_NonExistingEngagementId_ReturnsFalse()
    {
        // Arrange
        var domainTalk = new Domain.Models.Talk { Name = "Talk" };

        // Act
        var result = await _dataStore.AddTalkToEngagementAsync(999, domainTalk);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_InvalidEngagementId_ThrowsApplicationException()
    {
        // Arrange
        var domainTalk = new Domain.Models.Talk { Name = "Talk" };

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _dataStore.AddTalkToEngagementAsync(0, domainTalk));
    }

    [Fact]
    public async Task AddTalkToEngagementAsync_NullTalkWithId_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _dataStore.AddTalkToEngagementAsync(1, null!));
    }

    [Fact]
    public async Task SaveTalkAsync_NullTalk_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _dataStore.SaveTalkAsync(null!));
    }

    [Fact]
    public async Task SaveTalkAsync_NewTalk_SavesAndReturns()
    {
        // Arrange
        var domainTalk = new Domain.Models.Talk
        {
            Id = 0,
            Name = "Brand New Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = new DateTimeOffset(2025, 6, 1, 10, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 6, 1, 11, 0, 0, TimeSpan.Zero)
        };

        // Act
        var result = await _dataStore.SaveTalkAsync(domainTalk);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal("Brand New Talk", result.Value!.Name);
    }

    [Fact]
    public async Task SaveTalkAsync_ExistingTalk_UpdatesAndReturns()
    {
        // Arrange
        var talk = CreateTalk(name: "Original Talk");
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();

        var domainTalk = new Domain.Models.Talk
        {
            Id = talk.Id,
            Name = "Updated Talk",
            UrlForConferenceTalk = "https://conf.example.com/talk",
            UrlForTalk = "https://example.com/talk",
            StartDateTime = talk.StartDateTime,
            EndDateTime = talk.EndDateTime
        };

        // Act
        var result = await _dataStore.SaveTalkAsync(domainTalk);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Updated Talk", result.Value!.Name);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_InvalidTalkId_ReturnsFailure()
    {
        // Act
        var result = await _dataStore.RemoveTalkFromEngagementAsync(0);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_NonExistingTalkId_ReturnsTrue()
    {
        // Act
        var result = await _dataStore.RemoveTalkFromEngagementAsync(999);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_ExistingTalkId_RemovesTalk()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var talk = CreateTalk(engagement.Id);
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.RemoveTalkFromEngagementAsync(talk.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Talks.ToList());
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_NullTalk_ReturnsFailure()
    {
        // Act
        var result = await _dataStore.RemoveTalkFromEngagementAsync((Domain.Models.Talk)null!);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task RemoveTalkFromEngagementAsync_WithDomainTalk_RemovesTalk()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var talk = CreateTalk(engagement.Id);
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();
        _context.Entry(talk).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var domainTalk = new Domain.Models.Talk
        {
            Id = talk.Id,
            Name = talk.Name,
            UrlForConferenceTalk = talk.UrlForConferenceTalk,
            UrlForTalk = talk.UrlForTalk,
            StartDateTime = talk.StartDateTime,
            EndDateTime = talk.EndDateTime
        };

        // Act
        var result = await _dataStore.RemoveTalkFromEngagementAsync(domainTalk);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(_context.Talks.ToList());
    }

    [Fact]
    public async Task GetTalkAsync_InvalidId_ThrowsApplicationException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() =>
            _dataStore.GetTalkAsync(0));
    }

    [Fact]
    public async Task GetTalkAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetTalkAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetTalkAsync_ExistingId_ReturnsTalk()
    {
        // Arrange
        var engagement = CreateEngagement();
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        var talk = CreateTalk(engagement.Id, "Specific Talk");
        _context.Talks.Add(talk);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetTalkAsync(talk.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Specific Talk", result.Name);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_MatchingEngagement_ReturnsEngagement()
    {
        // Arrange
        var engagement = new Engagement
        {
            Name = "Exact Conf",
            Url = "https://exactconf.com",
            StartDateTime = new DateTimeOffset(2025, 5, 10, 9, 0, 0, TimeSpan.Zero),
            EndDateTime = new DateTimeOffset(2025, 5, 12, 17, 0, 0, TimeSpan.Zero),
            TimeZoneId = "UTC"
        };
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByNameAndUrlAndYearAsync("Exact Conf", "https://exactconf.com", 2025);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Exact Conf", result.Name);
    }

    [Fact]
    public async Task GetByNameAndUrlAndYearAsync_NoMatch_ReturnsNull()
    {
        // Act
        var result = await _dataStore.GetByNameAndUrlAndYearAsync("Nonexistent", "https://none.com", 2025);

        // Assert
        Assert.Null(result);
    }
}
