using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class SyndicationFeedSourcesControllerTests
{
    private readonly Mock<ISyndicationFeedSourceManager> _managerMock;
    private readonly Mock<ILogger<SyndicationFeedSourcesController>> _loggerMock;

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public SyndicationFeedSourcesControllerTests()
    {
        _managerMock = new Mock<ISyndicationFeedSourceManager>();
        _loggerMock = new Mock<ILogger<SyndicationFeedSourcesController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SyndicationFeedSourcesController CreateSut(string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        var controller = new SyndicationFeedSourcesController(
            _managerMock.Object,
            _loggerMock.Object,
            _mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    private static SyndicationFeedSource BuildSource(int id = 1, string oid = "owner-oid-12345") => new()
    {
        Id = id,
        FeedIdentifier = $"feed-{id}",
        Author = $"Author {id}",
        Title = $"Feed Source {id}",
        Url = $"https://feed-{id}.example.com",
        PublicationDate = DateTimeOffset.UtcNow.AddDays(-id),
        AddedOn = DateTimeOffset.UtcNow.AddDays(-id),
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = oid
    };

    private static SyndicationFeedSourceRequest BuildRequest() => new()
    {
        FeedIdentifier = "new-feed-123",
        Author = "New Author",
        Title = "New Feed Source",
        Url = "https://new-feed.example.com",
        PublicationDate = DateTimeOffset.UtcNow.AddDays(-1)
    };

    // -------------------------------------------------------------------------
    // GetAllAsync — GET /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNotSiteAdmin_CallsOwnerFilteredGetAll()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { BuildSource(1), BuildSource(2) };
        _managerMock
            .Setup(m => m.GetAllAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SyndicationFeedSource> { Items = sources, TotalCount = sources.Count });

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: false);

        // Act
        var result = await sut.GetAllAsync();

        // Assert — controller returns PagedResponse<> directly (implicit ActionResult<T> conversion)
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().HaveCount(2);

        // Owner-filtered overload must fire …
        _managerMock.Verify(m => m.GetAllAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // … and the unfiltered overload must never be called.
        _managerMock.Verify(m => m.GetAllAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenSiteAdmin_CallsUnfilteredGetAll()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { BuildSource(1) };
        _managerMock
            .Setup(m => m.GetAllAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SyndicationFeedSource> { Items = sources, TotalCount = sources.Count });

        var sut = CreateSut(isSiteAdmin: true);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);
        result.Value.Items.Should().HaveCount(1);

        // Unfiltered overload must fire …
        _managerMock.Verify(m => m.GetAllAsync(
            It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
        // … and the owner-filtered overload must never be called.
        _managerMock.Verify(m => m.GetAllAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        // Arrange
        _managerMock
            .Setup(m => m.GetAllAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<SyndicationFeedSource> { Items = [], TotalCount = 0 });

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // GetSyndicationFeedSourceAsync — GET /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSyndicationFeedSourceAsync_WhenFound_ReturnsSource()
    {
        // Arrange
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        // Act
        var result = await sut.GetSyndicationFeedSourceAsync(5);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<SyndicationFeedSourceResponse>()
            .Which.Id.Should().Be(5);
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSyndicationFeedSourceAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _managerMock.Setup(m => m.GetAsync(99, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SyndicationFeedSource?>(null));

        var sut = CreateSut();

        // Act
        var result = await sut.GetSyndicationFeedSourceAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.GetAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSyndicationFeedSourceAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange — source owned by "owner-oid-12345"; caller has a different OID.
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetSyndicationFeedSourceAsync(5);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetSyndicationFeedSourceAsync_WhenSiteAdmin_BypassesOwnerCheck()
    {
        // Arrange — source owned by someone else, but caller is a site admin.
        var source = BuildSource(5, oid: "some-other-oid");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: true);

        // Act
        var result = await sut.GetSyndicationFeedSourceAsync(5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateSyndicationFeedSourceAsync — POST /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateSyndicationFeedSourceAsync_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = BuildRequest();
        var saved = BuildSource(10);
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(saved));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateSyndicationFeedSourceAsync(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SyndicationFeedSourcesController.GetSyndicationFeedSourceAsync));
        createdResult.Value.Should().BeAssignableTo<SyndicationFeedSourceResponse>()
            .Which.Id.Should().Be(10);
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSyndicationFeedSourceAsync_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await sut.CreateSyndicationFeedSourceAsync(new SyndicationFeedSourceRequest
        {
            FeedIdentifier = "x",
            Author = "A",
            Title = string.Empty,
            Url = "https://example.com",
            PublicationDate = DateTimeOffset.UtcNow
        });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSyndicationFeedSourceAsync_WhenSaveFails_ReturnsProblem()
    {
        // Arrange
        var request = BuildRequest();
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<SyndicationFeedSource>.Failure("Save failed"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateSyndicationFeedSourceAsync(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSyndicationFeedSourceAsync_WhenCalled_StampsCreatedByEntraOidFromClaims()
    {
        // Arrange
        var request = BuildRequest();
        SyndicationFeedSource? captured = null;
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<SyndicationFeedSource>(), It.IsAny<CancellationToken>()))
            .Callback<SyndicationFeedSource, CancellationToken>((src, _) => captured = src)
            .ReturnsAsync(OperationResult<SyndicationFeedSource>.Success(BuildSource(1, oid: "owner-oid-12345")));

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        // Act
        var result = await sut.CreateSyndicationFeedSourceAsync(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        captured.Should().NotBeNull();
        captured!.CreatedByEntraOid.Should().Be("owner-oid-12345");
    }

    // -------------------------------------------------------------------------
    // DeleteSyndicationFeedSourceAsync — DELETE /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteSyndicationFeedSourceAsync_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var source = BuildSource(3);
        _managerMock.Setup(m => m.GetAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _managerMock.Setup(m => m.DeleteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<bool>.Success(true));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteSyndicationFeedSourceAsync(3);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSyndicationFeedSourceAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _managerMock.Setup(m => m.GetAsync(99, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SyndicationFeedSource?>(null));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteSyndicationFeedSourceAsync(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.GetAsync(99, It.IsAny<CancellationToken>()), Times.Once);
        _managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSyndicationFeedSourceAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange — source owned by "owner-oid-12345"; caller has a different OID.
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.DeleteSyndicationFeedSourceAsync(5);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteSyndicationFeedSourceAsync_WhenSiteAdmin_BypassesOwnerCheck()
    {
        // Arrange — source owned by someone else, but caller is a site admin.
        var source = BuildSource(5, oid: "some-other-oid");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _managerMock.Setup(m => m.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<bool>.Success(true));

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: true);

        // Act
        var result = await sut.DeleteSyndicationFeedSourceAsync(5);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
