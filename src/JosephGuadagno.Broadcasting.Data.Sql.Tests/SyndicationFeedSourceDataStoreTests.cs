using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class SyndicationFeedSourceDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly SyndicationFeedSourceDataStore _dataStore;

    public SyndicationFeedSourceDataStoreTests()
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

        _dataStore = new SyndicationFeedSourceDataStore(_context, mapper, NullLogger<SyndicationFeedSourceDataStore>.Instance);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private void SeedData()
    {
        var sources = new List<SyndicationFeedSource>
        {
            new SyndicationFeedSource
            {
                Id = 1,
                FeedIdentifier = "post1",
                Title = "Post 1",
                Author = "Author",
                Url = "url1",
                Tags = "csharp,azure",
                PublicationDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            },
            new SyndicationFeedSource
            {
                Id = 2,
                FeedIdentifier = "post2",
                Title = "Post 2",
                Author = "Author",
                Url = "url2",
                Tags = "dotnet,aws",
                PublicationDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            },
            new SyndicationFeedSource
            {
                Id = 3,
                FeedIdentifier = "post3",
                Title = "Post 3",
                Author = "Author",
                Url = "url3",
                Tags = "csharp,dotnet",
                PublicationDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ItemLastUpdatedOn = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            },
            new SyndicationFeedSource
            {
                Id = 4,
                FeedIdentifier = "post4",
                Title = "Post 4",
                Author = "Author",
                Url = "url4",
                Tags = "java",
                PublicationDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            },
            new SyndicationFeedSource
            {
                Id = 5,
                FeedIdentifier = "post5",
                Title = "Post 5",
                Author = "Author",
                Url = "url5",
                Tags = null,
                PublicationDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                AddedOn = DateTimeOffset.UtcNow,
                LastUpdatedOn = DateTimeOffset.UtcNow
            }
        };
        _context.SyndicationFeedSources.AddRange(sources);
        _context.SaveChanges();

        // Seed SourceTags junction records mirroring the string Tags above
        _context.SourceTags.AddRange(
            new Models.SourceTag { SourceId = 1, SourceType = "SyndicationFeed", Tag = "csharp" },
            new Models.SourceTag { SourceId = 1, SourceType = "SyndicationFeed", Tag = "azure" },
            new Models.SourceTag { SourceId = 2, SourceType = "SyndicationFeed", Tag = "dotnet" },
            new Models.SourceTag { SourceId = 2, SourceType = "SyndicationFeed", Tag = "aws" },
            new Models.SourceTag { SourceId = 3, SourceType = "SyndicationFeed", Tag = "csharp" },
            new Models.SourceTag { SourceId = 3, SourceType = "SyndicationFeed", Tag = "dotnet" },
            new Models.SourceTag { SourceId = 4, SourceType = "SyndicationFeed", Tag = "java" }
        );
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ReturnsRecord_WhenCutoffDateMatchesPublicationDate()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, new List<string>());

        Assert.NotNull(result);
        Assert.True(result.PublicationDate >= cutoffDate || result.ItemLastUpdatedOn >= cutoffDate);
        // Should be Id 1, 3 (ItemLastUpdatedOn in 2025), or 5 (PublicationDate = 2025-01-01)
        Assert.Contains(result.Id, new[] { 1, 3, 5 });
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ReturnsRecord_WhenCutoffDateMatchesItemLastUpdatedOn()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero);
        
        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, new List<string>());

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ExcludesCategories()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2023, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var excludedCategories = new List<string> { "csharp" };

        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        Assert.NotNull(result);
        if (result.Tags.Count > 0)
        {
            Assert.DoesNotContain("csharp", result.Tags);
        }
        // Post 1 and 3 are excluded because they have 'csharp'
        // Post 4 is excluded by date (2020 < 2023)
        // Post 2 and 5 are included
        Assert.Contains(result.Id, new[] { 2, 5 });
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ExcludesMultipleCategories()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var excludedCategories = new List<string> { "csharp", "dotnet" };

        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        Assert.NotNull(result);
        if (result.Tags.Count > 0)
        {
            Assert.DoesNotContain("csharp", result.Tags);
            Assert.DoesNotContain("dotnet", result.Tags);
        }
        // Post 1, 2, 3 are excluded because they have either 'csharp' or 'dotnet'
        // Post 4 and 5 are included
        Assert.Contains(result.Id, new[] { 4, 5 });
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_HandlesNullTags()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var excludedCategories = new List<string> { "csharp" };

        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, excludedCategories);

        Assert.NotNull(result);
        // Post 1 is excluded (has csharp)
        // Post 3 is excluded (has csharp)
        // Post 5 is included (Tags is null, 2025 >= 2025)
        Assert.Equal(5, result.Id);
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ReturnsNull_WhenNoRecordMatches()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, new List<string>());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_ReturnsRecord_WhenIdExists()
    {
        SeedData();
        var result = await _dataStore.GetAsync(1);
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenIdDoesNotExist()
    {
        SeedData();
        var result = await _dataStore.GetAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllRecords()
    {
        SeedData();
        var result = await _dataStore.GetAllAsync();
        Assert.NotNull(result);
        Assert.Equal(5, result.Count);
    }

    [Fact]
    public async Task GetByUrlAsync_ReturnsRecord_WhenUrlExists()
    {
        SeedData();
        var result = await _dataStore.GetByUrlAsync("url1");
        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task GetByUrlAsync_ReturnsNull_WhenUrlDoesNotExist()
    {
        SeedData();
        var result = await _dataStore.GetByUrlAsync("non-existing-url");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_AddsNewRecord_WhenIdIsZero()
    {
        var newSource = new JosephGuadagno.Broadcasting.Domain.Models.SyndicationFeedSource
        {
            FeedIdentifier = "newpost",
            Title = "New Post",
            Author = "Author",
            Url = "newurl",
            Tags = ["tag"],
            PublicationDate = DateTimeOffset.UtcNow
        };

        var result = await _dataStore.SaveAsync(newSource);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.Id > 0);
        var dbRecord = await _context.SyndicationFeedSources.FindAsync(result.Value!.Id);
        Assert.NotNull(dbRecord);
        Assert.Equal("New Post", dbRecord.Title);
    }

    [Fact]
    public async Task SaveAsync_UpdatesRecord_WhenIdIsNotZero()
    {
        SeedData();
        var existing = await _dataStore.GetAsync(1);
        _context.ChangeTracker.Clear();
        existing.Title = "Updated Title";

        var result = await _dataStore.SaveAsync(existing);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal("Updated Title", result.Value!.Title);
        var dbRecord = await _context.SyndicationFeedSources.FindAsync(1);
        Assert.Equal("Updated Title", dbRecord.Title);
    }

    [Fact]
    public async Task DeleteAsync_WithId_ReturnsTrue_WhenIdExists()
    {
        SeedData();
        var result = await _dataStore.DeleteAsync(1);
        Assert.True(result.IsSuccess);
        var dbRecord = await _context.SyndicationFeedSources.FindAsync(1);
        Assert.Null(dbRecord);
    }

    [Fact]
    public async Task DeleteAsync_WithId_ReturnsTrue_WhenIdDoesNotExist()
    {
        SeedData();
        var result = await _dataStore.DeleteAsync(999);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DeleteAsync_WithEntity_ReturnsTrue_WhenEntityExists()
    {
        SeedData();
        var existing = await _dataStore.GetAsync(1);
        var result = await _dataStore.DeleteAsync(existing);
        Assert.True(result.IsSuccess);
        var dbRecord = await _context.SyndicationFeedSources.FindAsync(1);
        Assert.Null(dbRecord);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ReturnsRecord_WhenFeedIdentifierExists()
    {
        SeedData();
        var result = await _dataStore.GetByFeedIdentifierAsync("post2");
        Assert.NotNull(result);
        Assert.Equal(2, result.Id);
        Assert.Equal("post2", result.FeedIdentifier);
    }

    [Fact]
    public async Task GetByFeedIdentifierAsync_ReturnsNull_WhenFeedIdentifierDoesNotExist()
    {
        SeedData();
        var result = await _dataStore.GetByFeedIdentifierAsync("non-existent-feed-identifier");
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThrowsApplicationException_WhenSaveReturnsZero()
    {
        var options = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        await using var failingContext = new FailingSaveBroadcastingContext(options);

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var mapper = config.CreateMapper();

        var store = new SyndicationFeedSourceDataStore(failingContext, mapper, NullLogger<SyndicationFeedSourceDataStore>.Instance);
        var entity = new JosephGuadagno.Broadcasting.Domain.Models.SyndicationFeedSource
        {
            FeedIdentifier = "fail",
            Title = "Should Fail",
            Author = "Author",
            Url = "fail-url",
            Tags = ["x"],
            PublicationDate = DateTimeOffset.UtcNow
        };

        var result = await store.SaveAsync(entity);
        Assert.False(result.IsSuccess);
    }
}

internal sealed class FailingSaveBroadcastingContext : BroadcastingContext
{
    public FailingSaveBroadcastingContext(DbContextOptions<BroadcastingContext> options) : base(options) { }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }
}