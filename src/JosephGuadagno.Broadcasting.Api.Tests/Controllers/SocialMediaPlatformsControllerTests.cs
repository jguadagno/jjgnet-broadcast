using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class SocialMediaPlatformsControllerTests
{
    private readonly Mock<ISocialMediaPlatformManager> _managerMock;
    private readonly Mock<ILogger<SocialMediaPlatformsController>> _loggerMock;

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public SocialMediaPlatformsControllerTests()
    {
        _managerMock = new Mock<ISocialMediaPlatformManager>();
        _loggerMock = new Mock<ILogger<SocialMediaPlatformsController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SocialMediaPlatformsController CreateSut()
    {
        var controller = new SocialMediaPlatformsController(
            _managerMock.Object,
            _loggerMock.Object,
            _mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    // -------------------------------------------------------------------------
    // GetAllAsync ΓÇö GET /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_ShouldReturn200WithPlatforms()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new() { Id = 1, Name = "Twitter",  Url = "https://twitter.com",  IsActive = true },
            new() { Id = 2, Name = "BlueSky",  Url = "https://bsky.app",     IsActive = true },
            new() { Id = 3, Name = "LinkedIn", Url = "https://linkedin.com", IsActive = true }
        };
        _managerMock.Setup(m => m.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(platforms);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<SocialMediaPlatformResponse>>().Subject;
        response.Should().HaveCount(3);
        response.Should().BeEquivalentTo(platforms, opts => opts.ExcludingMissingMembers());
        _managerMock.Verify(m => m.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenEmpty_ShouldReturn200WithEmptyList()
    {
        // Arrange
        _managerMock.Setup(m => m.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SocialMediaPlatform>());

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<SocialMediaPlatformResponse>>().Subject;
        response.Should().BeEmpty();
        _managerMock.Verify(m => m.GetAllAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetAsync ΓÇö GET /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenFound_ShouldReturn200()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 42, Name = "Mastodon", Url = "https://mastodon.social", IsActive = true };
        _managerMock.Setup(m => m.GetByIdAsync(42, It.IsAny<CancellationToken>())).ReturnsAsync(platform);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync(42);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<SocialMediaPlatformResponse>().Subject;
        response.Id.Should().Be(42);
        response.Name.Should().Be("Mastodon");
        response.Url.Should().Be("https://mastodon.social");
        response.IsActive.Should().BeTrue();
        _managerMock.Verify(m => m.GetByIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _managerMock.Setup(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.GetByIdAsync(99, It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateAsync ΓÇö POST /
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new SocialMediaPlatformRequest
        {
            Name    = "Pinterest",
            Url     = "https://pinterest.com",
            Icon    = "bi-pinterest",
            IsActive = true
        };
        var created = new SocialMediaPlatform
        {
            Id       = 10,
            Name     = "Pinterest",
            Url      = "https://pinterest.com",
            Icon     = "bi-pinterest",
            IsActive = true
        };
        _managerMock
            .Setup(m => m.AddAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.GetAsync));

        var response = createdResult.Value.Should().BeAssignableTo<SocialMediaPlatformResponse>().Subject;
        response.Id.Should().Be(10);
        response.Name.Should().Be("Pinterest");
        _managerMock.Verify(m => m.AddAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidModelState_ShouldReturn400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.CreateAsync(new SocialMediaPlatformRequest { Name = string.Empty });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.AddAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenManagerFails_ShouldReturn400()
    {
        // Arrange
        var request = new SocialMediaPlatformRequest { Name = "FailPlatform", IsActive = true };
        _managerMock
            .Setup(m => m.AddAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.AddAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateAsync ΓÇö PUT /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WithValidRequest_ShouldReturn200()
    {
        // Arrange
        var request = new SocialMediaPlatformRequest
        {
            Name     = "Twitter / X",
            Url      = "https://x.com",
            Icon     = "bi-twitter-x",
            IsActive = true
        };
        var updated = new SocialMediaPlatform
        {
            Id       = 5,
            Name     = "Twitter / X",
            Url      = "https://x.com",
            Icon     = "bi-twitter-x",
            IsActive = true
        };
        _managerMock
            .Setup(m => m.UpdateAsync(It.Is<SocialMediaPlatform>(p => p.Id == 5), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateAsync(5, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<SocialMediaPlatformResponse>().Subject;
        response.Id.Should().Be(5);
        response.Name.Should().Be("Twitter / X");
        _managerMock.Verify(m => m.UpdateAsync(It.Is<SocialMediaPlatform>(p => p.Id == 5), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        var request = new SocialMediaPlatformRequest { Name = "Ghost Platform", IsActive = false };
        _managerMock
            .Setup(m => m.UpdateAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.UpdateAsync(999, request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.UpdateAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidModelState_ShouldReturn400()
    {
        // Arrange
        var sut = CreateSut();
        sut.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await sut.UpdateAsync(1, new SocialMediaPlatformRequest { Name = string.Empty });

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.UpdateAsync(It.IsAny<SocialMediaPlatform>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // DeleteAsync ΓÇö DELETE /{id}
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteAsync_WhenFound_ShouldReturn204()
    {
        // Arrange
        _managerMock.Setup(m => m.DeleteAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAsync(7);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_ShouldReturn404()
    {
        // Arrange
        _managerMock.Setup(m => m.DeleteAsync(404, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();

        // Act
        var result = await sut.DeleteAsync(404);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
        _managerMock.Verify(m => m.DeleteAsync(404, It.IsAny<CancellationToken>()), Times.Once);
    }
}
