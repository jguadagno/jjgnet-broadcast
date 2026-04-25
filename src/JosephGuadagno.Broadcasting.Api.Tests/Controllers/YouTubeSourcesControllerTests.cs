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

public class YouTubeSourcesControllerTests
{
    private readonly Mock<IYouTubeSourceManager> _managerMock;
    private readonly Mock<ILogger<YouTubeSourcesController>> _loggerMock;

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public YouTubeSourcesControllerTests()
    {
        _managerMock = new Mock<IYouTubeSourceManager>();
        _loggerMock = new Mock<ILogger<YouTubeSourcesController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private YouTubeSourcesController CreateSut(string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        var controller = new YouTubeSourcesController(
            _managerMock.Object,
            _loggerMock.Object,
            _mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    private static YouTubeSource BuildSource(int id = 1, string oid = "owner-oid-12345") => new()
    {
        Id = id,
        VideoId = $"video-{id}",
        Author = $"Author {id}",
        Title = $"YouTube Source {id}",
        Url = $"https://youtube.com/watch?v=video-{id}",
        PublicationDate = DateTimeOffset.UtcNow.AddDays(-id),
        AddedOn = DateTimeOffset.UtcNow.AddDays(-id),
        LastUpdatedOn = DateTimeOffset.UtcNow,
        CreatedByEntraOid = oid
    };

    private static YouTubeSourceRequest BuildRequest() => new()
    {
        VideoId = "new-video-123",
        Author = "New Author",
        Title = "New YouTube Source",
        Url = "https://youtube.com/watch?v=new-video-123",
        PublicationDate = DateTimeOffset.UtcNow.AddDays(-1)
    };

    // -------------------------------------------------------------------------
    // GetAllAsync — GET /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNotSiteAdmin_CallsOwnerFilteredGetAll()
    {
        // Arrange
        var sources = new List<YouTubeSource> { BuildSource(1), BuildSource(2) };
        _managerMock
            .Setup(m => m.GetAllAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<YouTubeSource> { Items = sources, TotalCount = sources.Count });

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
        var sources = new List<YouTubeSource> { BuildSource(1) };
        _managerMock
            .Setup(m => m.GetAllAsync(
                It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<YouTubeSource> { Items = sources, TotalCount = sources.Count });

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
            .ReturnsAsync(new PagedResult<YouTubeSource> { Items = [], TotalCount = 0 });

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    // -------------------------------------------------------------------------
    // GetYouTubeSourceAsync — GET /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetYouTubeSourceAsync_WhenFound_ReturnsSource()
    {
        // Arrange
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        // Act
        var result = await sut.GetYouTubeSourceAsync(5);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<YouTubeSourceResponse>()
            .Which.Id.Should().Be(5);
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetYouTubeSourceAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _managerMock.Setup(m => m.GetAsync(99, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<YouTubeSource?>(null));

        var sut = CreateSut();

        // Act
        var result = await sut.GetYouTubeSourceAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.GetAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetYouTubeSourceAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange — source owned by "owner-oid-12345"; caller has a different OID.
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetYouTubeSourceAsync(5);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetYouTubeSourceAsync_WhenSiteAdmin_BypassesOwnerCheck()
    {
        // Arrange — source owned by someone else, but caller is a site admin.
        var source = BuildSource(5, oid: "some-other-oid");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: true);

        // Act
        var result = await sut.GetYouTubeSourceAsync(5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _managerMock.Verify(m => m.GetAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateYouTubeSourceAsync — POST /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateYouTubeSourceAsync_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = BuildRequest();
        var saved = BuildSource(10);
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<YouTubeSource>.Success(saved));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateYouTubeSourceAsync(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(YouTubeSourcesController.GetYouTubeSourceAsync));
        createdResult.Value.Should().BeAssignableTo<YouTubeSourceResponse>()
            .Which.Id.Should().Be(10);
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateYouTubeSourceAsync_WithInvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Title", "Title is required");

        // Act
        var result = await sut.CreateYouTubeSourceAsync(new YouTubeSourceRequest
        {
            VideoId = "v123",
            Author = "A",
            Title = string.Empty,
            Url = "https://youtube.com/watch?v=v123",
            PublicationDate = DateTimeOffset.UtcNow
        });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateYouTubeSourceAsync_WhenSaveFails_ReturnsProblem()
    {
        // Arrange
        var request = BuildRequest();
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<YouTubeSource>.Failure("Save failed"));

        var sut = CreateSut();

        // Act
        var result = await sut.CreateYouTubeSourceAsync(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateYouTubeSourceAsync_WhenCalled_StampsCreatedByEntraOidFromClaims()
    {
        // Arrange
        var request = BuildRequest();
        YouTubeSource? captured = null;
        _managerMock
            .Setup(m => m.SaveAsync(It.IsAny<YouTubeSource>(), It.IsAny<CancellationToken>()))
            .Callback<YouTubeSource, CancellationToken>((src, _) => captured = src)
            .ReturnsAsync(OperationResult<YouTubeSource>.Success(BuildSource(1, oid: "owner-oid-12345")));

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        // Act
        var result = await sut.CreateYouTubeSourceAsync(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        captured.Should().NotBeNull();
        captured!.CreatedByEntraOid.Should().Be("owner-oid-12345");
    }

    // -------------------------------------------------------------------------
    // DeleteYouTubeSourceAsync — DELETE /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteYouTubeSourceAsync_WhenFound_ReturnsNoContent()
    {
        // Arrange
        var source = BuildSource(3);
        _managerMock.Setup(m => m.GetAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _managerMock.Setup(m => m.DeleteAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<bool>.Success(true));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteYouTubeSourceAsync(3);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteYouTubeSourceAsync_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        _managerMock.Setup(m => m.GetAsync(99, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<YouTubeSource?>(null));

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteYouTubeSourceAsync(99);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.GetAsync(99, It.IsAny<CancellationToken>()), Times.Once);
        _managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteYouTubeSourceAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange — source owned by "owner-oid-12345"; caller has a different OID.
        var source = BuildSource(5, oid: "owner-oid-12345");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.DeleteYouTubeSourceAsync(5);

        // Assert
        result.Should().BeOfType<ForbidResult>();
        _managerMock.Verify(m => m.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteYouTubeSourceAsync_WhenSiteAdmin_BypassesOwnerCheck()
    {
        // Arrange — source owned by someone else, but caller is a site admin.
        var source = BuildSource(5, oid: "some-other-oid");
        _managerMock.Setup(m => m.GetAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(source);
        _managerMock.Setup(m => m.DeleteAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<bool>.Success(true));

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: true);

        // Act
        var result = await sut.DeleteYouTubeSourceAsync(5);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }
}
