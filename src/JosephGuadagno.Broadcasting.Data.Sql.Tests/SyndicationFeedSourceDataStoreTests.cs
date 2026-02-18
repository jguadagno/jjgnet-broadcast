using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var mapper = config.CreateMapper();

        _dataStore = new SyndicationFeedSourceDataStore(_context, mapper);
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
    }

    [Fact]
    public async Task GetRandomSyndicationDataAsync_ReturnsRecord_WhenCutoffDateMatchesPublicationDate()
    {
        SeedData();
        var cutoffDate = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        
        var result = await _dataStore.GetRandomSyndicationDataAsync(cutoffDate, new List<string>());

        Assert.NotNull(result);
        Assert.True(result.PublicationDate >= cutoffDate || result.ItemLastUpdatedOn >= cutoffDate);
        // Should be Id 1 or 3 (3 has ItemLastUpdatedOn in 2025)
        Assert.Contains(result.Id, new[] { 1, 3 });
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
        if (result.Tags != null)
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
        if (result.Tags != null)
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
}