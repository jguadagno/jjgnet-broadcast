using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Exceptions;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

/// <summary>
/// Unit tests for the social media platform sub-resource endpoints on
/// <see cref="EngagementsController"/>:
///   GET    /engagements/{id}/platforms
///   POST   /engagements/{id}/platforms
///   DELETE /engagements/{id}/platforms/{platformId}
/// </summary>
public class EngagementsController_PlatformsTests
{
    // =========================================================================
    // Fields / constructor
    // =========================================================================

    private readonly Mock<IEngagementManager> _engagementManagerMock;
    private readonly Mock<IEngagementSocialMediaPlatformDataStore> _dataStoreMock;
    private readonly Mock<ILogger<EngagementsController>> _loggerMock;

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public EngagementsController_PlatformsTests()
    {
        _engagementManagerMock = new Mock<IEngagementManager>();
        _dataStoreMock = new Mock<IEngagementSocialMediaPlatformDataStore>();
        _loggerMock = new Mock<ILogger<EngagementsController>>();

        // Every platform endpoint fetches the engagement first for the ownership check.
        // Set up a default that returns a valid owned engagement for any ID so tests
        // don't need per-test boilerplate.  Individual tests may override for specific IDs.
        _engagementManagerMock
            .Setup(m => m.GetAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns((int id, CancellationToken _) => Task.FromResult<Engagement>(BuildEngagement(id)));
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private EngagementsController CreateSut(string ownerOid = "owner-oid-12345")
    {
        return new EngagementsController(
            _engagementManagerMock.Object,
            _dataStoreMock.Object,
            _loggerMock.Object,
            _mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(ownerOid),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }


    /// <summary>
    /// Builds a minimal <see cref="Engagement"/> with <see cref="Engagement.CreatedByEntraOid"/>
    /// matching the default owner OID set in <see cref="CreateSut"/>.
    /// Used as the default return value of <c>GetAsync</c> so the per-endpoint ownership check passes.
    /// </summary>
    private static Engagement BuildEngagement(int id, string oid = "owner-oid-12345") => new()
    {
        Id = id,
        Name = $"Conference {id}",
        Url = $"https://conf-{id}.example.com",
        StartDateTime = DateTimeOffset.UtcNow,
        EndDateTime = DateTimeOffset.UtcNow.AddDays(id),
        TimeZoneId = "UTC",
        CreatedByEntraOid = oid
    };

    /// <summary>Builds a minimal <see cref="EngagementSocialMediaPlatform"/> for testing.</summary>
    private static EngagementSocialMediaPlatform BuildEsmp(
        int engagementId = 1,
        int platformId = 2,
        string? handle = "@TestHandle",
        string platformName = "Twitter") =>
        new()
        {
            EngagementId = engagementId,
            SocialMediaPlatformId = platformId,
            Handle = handle,
            SocialMediaPlatform = new SocialMediaPlatform
            {
                Id = platformId,
                Name = platformName,
                Url = "https://twitter.com",
                Icon = "bi-twitter-x",
                IsActive = true
            }
        };

    // =========================================================================
    // GET /engagements/{engagementId}/platforms
    // =========================================================================

    [Fact]
    public async Task GetPlatformsForEngagement_WhenEngagementHasPlatforms_ShouldReturn200WithList()
    {
        // Arrange
        var platforms = new List<EngagementSocialMediaPlatform>
        {
            BuildEsmp(1, 2, "@NDCSydney", "Twitter"),
            BuildEsmp(1, 3, "NDCSydney", "LinkedIn")
        };
        _dataStoreMock
            .Setup(d => d.GetByEngagementIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(platforms);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPlatformsForEngagementAsync(1);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responses = okResult.Value.Should()
            .BeAssignableTo<List<EngagementSocialMediaPlatformResponse>>().Subject;

        responses.Should().HaveCount(2);
        responses[0].Handle.Should().Be("@NDCSydney");
        responses[0].EngagementId.Should().Be(1);
        responses[0].SocialMediaPlatformId.Should().Be(2);
        responses[1].Handle.Should().Be("NDCSydney");
        responses[1].SocialMediaPlatformId.Should().Be(3);

        _dataStoreMock.Verify(d => d.GetByEngagementIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlatformsForEngagement_WhenNoPlatformsExist_ShouldReturn200WithEmptyList()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.GetByEngagementIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EngagementSocialMediaPlatform>());

        var sut = CreateSut();

        // Act
        var result = await sut.GetPlatformsForEngagementAsync(99);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responses = okResult.Value.Should()
            .BeAssignableTo<List<EngagementSocialMediaPlatformResponse>>().Subject;

        responses.Should().BeEmpty();
        _dataStoreMock.Verify(d => d.GetByEngagementIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlatformsForEngagement_ShouldMapHandleAndPlatformDetails_WhenPlatformNavigationIsPresent()
    {
        // Arrange
        var esmp = BuildEsmp(5, 10, "#DevConf2024", "Mastodon");
        _dataStoreMock
            .Setup(d => d.GetByEngagementIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EngagementSocialMediaPlatform> { esmp });

        var sut = CreateSut();

        // Act
        var result = await sut.GetPlatformsForEngagementAsync(5);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var responses = okResult.Value.Should()
            .BeAssignableTo<List<EngagementSocialMediaPlatformResponse>>().Subject;

        responses.Should().HaveCount(1);
        var first = responses[0];
        first.Handle.Should().Be("#DevConf2024");
        first.SocialMediaPlatform.Should().NotBeNull();
        first.SocialMediaPlatform!.Name.Should().Be("Mastodon");
        first.SocialMediaPlatform.Icon.Should().Be("bi-twitter-x");
    }

    // =========================================================================
    // GET /engagements/{engagementId}/platforms/{platformId}
    // =========================================================================

    [Fact]
    public async Task GetPlatformForEngagement_WhenAssociationExists_ShouldReturn200WithPlatform()
    {
        // Arrange
        var esmp = BuildEsmp(1, 2, "@NDCSydney", "Twitter");
        _dataStoreMock
            .Setup(d => d.GetAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(esmp);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPlatformForEngagementAsync(1, 2);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should()
            .BeOfType<EngagementSocialMediaPlatformResponse>().Subject;

        response.EngagementId.Should().Be(1);
        response.SocialMediaPlatformId.Should().Be(2);
        response.Handle.Should().Be("@NDCSydney");
        response.SocialMediaPlatform.Should().NotBeNull();
        response.SocialMediaPlatform!.Name.Should().Be("Twitter");

        _dataStoreMock.Verify(d => d.GetAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlatformForEngagement_WhenAssociationDoesNotExist_ShouldReturn404NotFound()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.GetAsync(1, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EngagementSocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetPlatformForEngagementAsync(1, 99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _dataStoreMock.Verify(d => d.GetAsync(1, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    // =========================================================================
    // POST /engagements/{engagementId}/platforms
    // =========================================================================

    [Fact]
    public async Task AddPlatformToEngagement_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        const int engagementId = 1;
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 2,
            Handle = "@NDCSydney"
        };
        var savedEsmp = BuildEsmp(engagementId, 2, "@NDCSydney");

        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEsmp);

        var sut = CreateSut();

        // Act
        var result = await sut.AddPlatformToEngagementAsync(engagementId, request);

        // Assert - Issue #708: CreatedAtAction should target single-item endpoint, not collection
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(EngagementsController.GetPlatformForEngagementAsync));
        createdResult.RouteValues.Should().ContainKey("engagementId")
            .WhoseValue.Should().Be(engagementId);
        createdResult.RouteValues.Should().ContainKey("platformId")
            .WhoseValue.Should().Be(2);

        var response = createdResult.Value.Should()
            .BeOfType<EngagementSocialMediaPlatformResponse>().Subject;
        response.EngagementId.Should().Be(engagementId);
        response.SocialMediaPlatformId.Should().Be(2);
        response.Handle.Should().Be("@NDCSydney");

        _dataStoreMock.Verify(
            d => d.AddAsync(It.Is<EngagementSocialMediaPlatform>(e =>
                e.EngagementId == engagementId && e.SocialMediaPlatformId == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPlatformToEngagement_WithNullHandle_ShouldReturn201Created()
    {
        // Arrange ΓÇö handle is optional per the domain model
        const int engagementId = 3;
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 5,
            Handle = null
        };
        var savedEsmp = BuildEsmp(engagementId, 5, null);

        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEsmp);

        var sut = CreateSut();

        // Act
        var result = await sut.AddPlatformToEngagementAsync(engagementId, request);

        // Assert - Issue #708: CreatedAtAction should target single-item endpoint with both IDs
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be("GetPlatformForEngagementAsync");
        createdResult.RouteValues.Should().ContainKey("engagementId")
            .WhoseValue.Should().Be(engagementId);
        createdResult.RouteValues.Should().ContainKey("platformId")
            .WhoseValue.Should().Be(5);
        
        var response = createdResult.Value.Should()
            .BeOfType<EngagementSocialMediaPlatformResponse>().Subject;
        response.Handle.Should().BeNull();
    }

    [Fact]
    public async Task AddPlatformToEngagement_WithInvalidModelState_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new EngagementSocialMediaPlatformRequest { SocialMediaPlatformId = 0 };
        var sut = CreateSut();
        sut.ModelState.AddModelError("SocialMediaPlatformId", "SocialMediaPlatformId is required");

        // Act
        var result = await sut.AddPlatformToEngagementAsync(1, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _dataStoreMock.Verify(
            d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPlatformToEngagement_WhenDataStoreReturnsNull_ShouldReturn500ProblemDetails()
    {
        // Arrange ΓÇö data store signals failure by returning null (e.g. DB constraint violation)
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 2,
            Handle = "@handle"
        };

        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EngagementSocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.AddPlatformToEngagementAsync(1, request);

        // Assert
        var problem = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);

        var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        details.Detail.Should().Be("Failed to add platform to engagement");
    }

    [Fact]
    public async Task AddPlatformToEngagement_WhenDuplicatePlatform_ShouldReturn409ConflictProblemDetails()
    {
        // Arrange ΓÇö duplicate is surfaced explicitly from the data store
        var request = new EngagementSocialMediaPlatformRequest { SocialMediaPlatformId = 2 };

        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateEngagementSocialMediaPlatformException(1, 2));

        var sut = CreateSut();

        // Act
        var result = await sut.AddPlatformToEngagementAsync(1, request);

        // Assert
        var problem = result.Result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        var details = problem.Value.Should().BeOfType<ProblemDetails>().Subject;
        details.Title.Should().Be("Platform already assigned");
        details.Detail.Should().Be("Engagement 1 already has social media platform 2 assigned.");
    }

    [Fact]
    public async Task AddPlatformToEngagement_WhenDuplicateAddIsAttempted_ShouldReturn409ConflictOnSecondRequest()
    {
        // Arrange ΓÇö Issue #708: first submit succeeds, duplicate follow-up is rejected
        const int engagementId = 1;
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 2,
            Handle = "@doubleclick"
        };
        var savedEsmp = BuildEsmp(engagementId, 2, "@doubleclick");

        _dataStoreMock
            .SetupSequence(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(savedEsmp)
            .ThrowsAsync(new DuplicateEngagementSocialMediaPlatformException(engagementId, request.SocialMediaPlatformId));

        var sut = CreateSut();

        // Act
        var firstResult = await sut.AddPlatformToEngagementAsync(engagementId, request);
        var secondResult = await sut.AddPlatformToEngagementAsync(engagementId, request);

        // Assert
        firstResult.Result.Should().BeOfType<CreatedAtActionResult>();

        var conflict = secondResult.Result.Should().BeOfType<ObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        conflict.Value.Should().BeOfType<ProblemDetails>()
            .Which.Detail.Should().Be("Engagement 1 already has social media platform 2 assigned.");

        _dataStoreMock.Verify(
            d => d.AddAsync(
                It.Is<EngagementSocialMediaPlatform>(e =>
                    e.EngagementId == engagementId &&
                    e.SocialMediaPlatformId == request.SocialMediaPlatformId &&
                    e.Handle == request.Handle),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task AddPlatformToEngagement_ShouldSetEngagementIdFromRoute_NotFromRequest()
    {
        // Arrange ΓÇö verify the route engagementId is stamped on the entity, not any request field
        const int routeEngagementId = 42;
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 7,
            Handle = "@RouteTest"
        };
        var savedEsmp = BuildEsmp(routeEngagementId, 7, "@RouteTest");

        EngagementSocialMediaPlatform? capturedEntity = null;
        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .Callback<EngagementSocialMediaPlatform, CancellationToken>((e, _) => capturedEntity = e)
            .ReturnsAsync(savedEsmp);

        var sut = CreateSut();

        // Act
        await sut.AddPlatformToEngagementAsync(routeEngagementId, request);

        // Assert
        capturedEntity.Should().NotBeNull();
        capturedEntity!.EngagementId.Should().Be(routeEngagementId);
        capturedEntity.SocialMediaPlatformId.Should().Be(7);
    }

    // =========================================================================
    // DELETE /engagements/{engagementId}/platforms/{platformId}
    // =========================================================================

    [Fact]
    public async Task RemovePlatformFromEngagement_WhenAssociationExists_ShouldReturn204NoContent()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.DeleteAsync(1, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.RemovePlatformFromEngagementAsync(1, 2);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _dataStoreMock.Verify(d => d.DeleteAsync(1, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePlatformFromEngagement_WhenAssociationDoesNotExist_ShouldReturn404NotFound()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.DeleteAsync(1, 99, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        var result = await sut.RemovePlatformFromEngagementAsync(1, 99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _dataStoreMock.Verify(d => d.DeleteAsync(1, 99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePlatformFromEngagement_ShouldPassBothIdsToDataStore()
    {
        // Arrange ΓÇö verify the correct composite key is forwarded to the data store
        _dataStoreMock
            .Setup(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.RemovePlatformFromEngagementAsync(7, 13);

        // Assert
        _dataStoreMock.Verify(
            d => d.DeleteAsync(7, 13, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // =========================================================================
    // Data store interaction ΓÇö verify no cross-contamination between operations
    // =========================================================================

    [Fact]
    public async Task GetPlatformsForEngagement_ShouldNeverCallAddOrDelete()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.GetByEngagementIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EngagementSocialMediaPlatform>());

        var sut = CreateSut();

        // Act
        await sut.GetPlatformsForEngagementAsync(1);

        // Assert
        _dataStoreMock.Verify(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Never);
        _dataStoreMock.Verify(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPlatformToEngagement_ShouldNeverCallGetOrDelete()
    {
        // Arrange
        var request = new EngagementSocialMediaPlatformRequest { SocialMediaPlatformId = 2, Handle = "@x" };
        _dataStoreMock
            .Setup(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildEsmp());

        var sut = CreateSut();

        // Act
        await sut.AddPlatformToEngagementAsync(1, request);

        // Assert
        _dataStoreMock.Verify(d => d.GetByEngagementIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _dataStoreMock.Verify(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemovePlatformFromEngagement_ShouldNeverCallGetOrAdd()
    {
        // Arrange
        _dataStoreMock
            .Setup(d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        await sut.RemovePlatformFromEngagementAsync(1, 2);

        // Assert
        _dataStoreMock.Verify(d => d.GetByEngagementIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _dataStoreMock.Verify(d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // =========================================================================
    // Security: non-owner ΓåÆ 403 ForbidResult (Platforms sub-actions)
    // The constructor default mock returns BuildEngagement(id) with OID "owner-oid-12345".
    // Creating the SUT with ownerOid "non-owner-oid-99999" ensures the ownership check fails.
    // =========================================================================

    [Fact]
    public async Task GetPlatformsForEngagementAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Default mock returns engagement owned by "owner-oid-12345"; user carries "non-owner-oid-99999".
        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetPlatformsForEngagementAsync(1);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _dataStoreMock.Verify(
            d => d.GetByEngagementIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPlatformForEngagementAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Default mock returns engagement owned by "owner-oid-12345"; user carries "non-owner-oid-99999".
        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetPlatformForEngagementAsync(1, 2);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _dataStoreMock.Verify(
            d => d.GetAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPlatformToEngagementAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Default mock returns engagement owned by "owner-oid-12345"; user carries "non-owner-oid-99999".
        var request = new EngagementSocialMediaPlatformRequest
        {
            SocialMediaPlatformId = 2,
            Handle = "@handle"
        };
        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.AddPlatformToEngagementAsync(1, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _dataStoreMock.Verify(
            d => d.AddAsync(It.IsAny<EngagementSocialMediaPlatform>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemovePlatformFromEngagementAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Default mock returns engagement owned by "owner-oid-12345"; user carries "non-owner-oid-99999".
        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.RemovePlatformFromEngagementAsync(1, 2);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _dataStoreMock.Verify(
            d => d.DeleteAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
