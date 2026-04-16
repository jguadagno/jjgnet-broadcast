using System.Reflection;
using AutoMapper;
using JosephGuadagno.Broadcasting.Data.Sql.Models;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JosephGuadagno.Broadcasting.Data.Sql.Tests;

public class EngagementSocialMediaPlatformDataStoreTests : IDisposable
{
    private readonly BroadcastingContext _context;
    private readonly EngagementSocialMediaPlatformDataStore _dataStore;
    private readonly Mock<ILogger<EngagementSocialMediaPlatformDataStore>> _loggerMock;

    public EngagementSocialMediaPlatformDataStoreTests()
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

        _loggerMock = new Mock<ILogger<EngagementSocialMediaPlatformDataStore>>();
        _dataStore = new EngagementSocialMediaPlatformDataStore(_context, mapper, _loggerMock.Object);
    }

    public void Dispose()
    {
        try
        {
            _context.Database.EnsureDeleted();
        }
        catch (ObjectDisposedException)
        {
            // Some tests intentionally dispose the context to verify exception behavior.
        }

        _context.Dispose();
    }

    private async Task<int> CreateEngagementAsync(string name = "Test Engagement")
    {
        var engagement = new Engagement
        {
            Name = name,
            Url = "https://example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC",
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };
        _context.Engagements.Add(engagement);
        await _context.SaveChangesAsync();
        return engagement.Id;
    }

    private async Task<int> CreateSocialMediaPlatformAsync(string name = "Twitter", bool isActive = true)
    {
        var platform = new SocialMediaPlatform
        {
            Name = name,
            IsActive = isActive
        };
        _context.SocialMediaPlatforms.Add(platform);
        await _context.SaveChangesAsync();
        return platform.Id;
    }

    private static EngagementSocialMediaPlatform CreateDbEngagementSocialMediaPlatform(
        int engagementId,
        int platformId,
        string? handle = null) => new()
    {
        EngagementId = engagementId,
        SocialMediaPlatformId = platformId,
        Handle = handle
    };

    #region GetByEngagementIdAsync Tests

    [Fact]
    public async Task GetByEngagementIdAsync_WhenPlatformsExist_ReturnsList()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platform1Id = await CreateSocialMediaPlatformAsync("Twitter");
        var platform2Id = await CreateSocialMediaPlatformAsync("LinkedIn");
        
        _context.EngagementSocialMediaPlatforms.AddRange(
            CreateDbEngagementSocialMediaPlatform(engagementId, platform1Id, "@testhandle"),
            CreateDbEngagementSocialMediaPlatform(engagementId, platform2Id, "@linkedinhandle")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.SocialMediaPlatformId == platform1Id);
        Assert.Contains(result, p => p.SocialMediaPlatformId == platform2Id);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_WhenNoPlatformsExist_ReturnsEmptyList()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_WhenDifferentEngagementHasPlatforms_ReturnsEmptyList()
    {
        // Arrange
        var engagement1Id = await CreateEngagementAsync("Engagement 1");
        var engagement2Id = await CreateEngagementAsync("Engagement 2");
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagement1Id, platformId)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagement2Id);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetByEngagementIdAsync_IncludesSocialMediaPlatformNavigation()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync("Bluesky");
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platformId, "@blueskyhandle")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetByEngagementIdAsync(engagementId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.NotNull(result[0].SocialMediaPlatform);
        Assert.Equal("Bluesky", result[0].SocialMediaPlatform!.Name);
    }

    #endregion

    #region GetAsync Tests

    [Fact]
    public async Task GetAsync_WhenAssociationExists_ReturnsAssociation()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync("Twitter");

        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platformId, "@existinghandle")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.GetAsync(engagementId, platformId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(engagementId, result.EngagementId);
        Assert.Equal(platformId, result.SocialMediaPlatformId);
        Assert.Equal("@existinghandle", result.Handle);
        Assert.NotNull(result.SocialMediaPlatform);
        Assert.Equal("Twitter", result.SocialMediaPlatform!.Name);
    }

    [Fact]
    public async Task GetAsync_WhenAssociationDoesNotExist_ReturnsNull()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync("Twitter");

        // Act
        var result = await _dataStore.GetAsync(engagementId, platformId);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region AddAsync Tests

    [Fact]
    public async Task AddAsync_WhenValid_AddsAndReturnsEntity()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        var domainPlatform = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = "@testhandle"
        };

        // Act
        var result = await _dataStore.AddAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(engagementId, result.EngagementId);
        Assert.Equal(platformId, result.SocialMediaPlatformId);
        Assert.Equal("@testhandle", result.Handle);

        var dbEntity = await _context.EngagementSocialMediaPlatforms
            .FirstOrDefaultAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platformId);
        Assert.NotNull(dbEntity);
    }

    [Fact]
    public async Task AddAsync_WhenHandleIsNull_AddsSuccessfully()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        var domainPlatform = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = null
        };

        // Act
        var result = await _dataStore.AddAsync(domainPlatform);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Handle);
    }

    [Fact]
    public async Task AddAsync_WhenAssociationAlreadyExists_ThrowsDuplicateExceptionAndKeepsExistingAssociation()
    {
        // Arrange — use a dedicated in-memory database so that both the seed context and the
        // mock context share the same state, but SaveChangesAsync is mocked to throw a
        // SQL Server-style DbUpdateException (simulating a PK constraint violation) so the
        // production catch block works correctly after the AnyAsync pre-check is removed.
        var sharedDbName = Guid.NewGuid().ToString();
        var sharedOptions = new DbContextOptionsBuilder<BroadcastingContext>()
            .UseInMemoryDatabase(sharedDbName)
            .Options;

        await using var seedContext = new BroadcastingContext(sharedOptions);
        var engagement = new Engagement
        {
            Name = "Test Engagement",
            Url = "https://example.com",
            StartDateTime = DateTimeOffset.UtcNow,
            EndDateTime = DateTimeOffset.UtcNow.AddDays(1),
            TimeZoneId = "UTC",
            CreatedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };
        seedContext.Engagements.Add(engagement);
        var platform = new SocialMediaPlatform { Name = "Twitter", IsActive = true };
        seedContext.SocialMediaPlatforms.Add(platform);
        await seedContext.SaveChangesAsync();

        seedContext.EngagementSocialMediaPlatforms.Add(new EngagementSocialMediaPlatform
        {
            EngagementId = engagement.Id,
            SocialMediaPlatformId = platform.Id,
            Handle = "@originalhandle"
        });
        await seedContext.SaveChangesAsync();

        // Mock context shares the same in-memory DB (AnyAsync pre-check still works while it
        // exists), but SaveChangesAsync throws the SQL Server PK violation exception so the
        // catch block in AddAsync triggers DuplicateEngagementSocialMediaPlatformException
        // both before and after the AnyAsync removal.
        var mockContext = new Mock<BroadcastingContext>(sharedOptions) { CallBase = true };
        mockContext
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException(
                "Violation of PRIMARY KEY constraint",
                CreateSqlExceptionForTesting(2627)));

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());
        var dataStore = new EngagementSocialMediaPlatformDataStore(
            mockContext.Object,
            config.CreateMapper(),
            _loggerMock.Object);

        var duplicateAssociation = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagement.Id,
            SocialMediaPlatformId = platform.Id,
            Handle = "@duplicatehandle"
        };

        // Act / Assert
        var exception = await Assert.ThrowsAsync<DuplicateEngagementSocialMediaPlatformException>(
            () => dataStore.AddAsync(duplicateAssociation));
        Assert.Equal(engagement.Id, exception.EngagementId);
        Assert.Equal(platform.Id, exception.SocialMediaPlatformId);

        seedContext.ChangeTracker.Clear();
        var persistedAssociations = await seedContext.EngagementSocialMediaPlatforms
            .Where(e => e.EngagementId == engagement.Id && e.SocialMediaPlatformId == platform.Id)
            .ToListAsync();

        Assert.Single(persistedAssociations);
        Assert.Equal("@originalhandle", persistedAssociations[0].Handle);
    }

    /// <summary>
    /// Creates a <see cref="SqlException"/> with the specified error number via reflection.
    /// <c>SqlException</c> has no public constructor; this helper is test-only and used to
    /// simulate SQL Server PK/unique-index constraint violations (error 2601 / 2627).
    /// </summary>
    private static SqlException CreateSqlExceptionForTesting(int errorNumber)
    {
        // Build a SqlError instance using the internal constructor.
        var errorType = typeof(SqlError);
        var errCtor = errorType
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(c => c.GetParameters().Length > 0 && c.GetParameters()[0].ParameterType == typeof(int))
            .OrderByDescending(c => c.GetParameters().Length)
            .First();

        var parms = errCtor.GetParameters();
        var ctorArgs = new object?[parms.Length];
        ctorArgs[0] = errorNumber;
        if (parms.Length > 1) ctorArgs[1] = (byte)0;
        if (parms.Length > 2) ctorArgs[2] = (byte)16;
        if (parms.Length > 3) ctorArgs[3] = "localhost";
        if (parms.Length > 4) ctorArgs[4] = $"SQL Server error {errorNumber}";
        if (parms.Length > 5) ctorArgs[5] = string.Empty;
        if (parms.Length > 6) ctorArgs[6] = 0;
        for (var i = 7; i < parms.Length; i++)
            ctorArgs[i] = parms[i].ParameterType == typeof(uint) ? (object)(uint)0 : null;

        var error = errCtor.Invoke(ctorArgs)!;

        // Add the error to a SqlErrorCollection.
        var collType = typeof(SqlErrorCollection);
        var coll = collType
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0]
            .Invoke(Array.Empty<object>());
        collType
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(coll, new[] { error });

        // Create the SqlException via the internal static factory method.
        var createMethod = typeof(SqlException)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "CreateException")
            .OrderBy(m => m.GetParameters().Length)
            .First();

        var mParms = createMethod.GetParameters();
        var mArgs = new object?[mParms.Length];
        mArgs[0] = coll;
        if (mParms.Length > 1) mArgs[1] = "7.0";

        return (SqlException)createMethod.Invoke(null, mArgs)!;
    }

    [Fact]
    public async Task AddAsync_WhenUnexpectedFailureOccurs_DoesNotSwallowTheException()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfiles.BroadcastingProfile>();
        }, new LoggerFactory());

        await _context.DisposeAsync();

        var disposedDataStore = new EngagementSocialMediaPlatformDataStore(
            _context,
            config.CreateMapper(),
            _loggerMock.Object);

        var association = new Domain.Models.EngagementSocialMediaPlatform
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = "@handle"
        };

        // Act / Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => disposedDataStore.AddAsync(association));
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WhenEntityExists_DeletesAndReturnsTrue()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platformId, "@deletetest")
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platformId);

        // Assert
        Assert.True(result);
        
        var dbEntity = await _context.EngagementSocialMediaPlatforms
            .FirstOrDefaultAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platformId);
        Assert.Null(dbEntity);
    }

    [Fact]
    public async Task DeleteAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platformId = await CreateSocialMediaPlatformAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platformId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_WhenEngagementIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var engagement1Id = await CreateEngagementAsync("Engagement 1");
        var engagement2Id = await CreateEngagementAsync("Engagement 2");
        var platformId = await CreateSocialMediaPlatformAsync();
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagement1Id, platformId)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagement2Id, platformId);

        // Assert
        Assert.False(result);
        
        var stillExists = await _context.EngagementSocialMediaPlatforms
            .AnyAsync(e => e.EngagementId == engagement1Id && e.SocialMediaPlatformId == platformId);
        Assert.True(stillExists);
    }

    [Fact]
    public async Task DeleteAsync_WhenPlatformIdDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var engagementId = await CreateEngagementAsync();
        var platform1Id = await CreateSocialMediaPlatformAsync("Twitter");
        var platform2Id = await CreateSocialMediaPlatformAsync("LinkedIn");
        
        _context.EngagementSocialMediaPlatforms.Add(
            CreateDbEngagementSocialMediaPlatform(engagementId, platform1Id)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _dataStore.DeleteAsync(engagementId, platform2Id);

        // Assert
        Assert.False(result);
        
        var stillExists = await _context.EngagementSocialMediaPlatforms
            .AnyAsync(e => e.EngagementId == engagementId && e.SocialMediaPlatformId == platform1Id);
        Assert.True(stillExists);
    }

    #endregion
}
