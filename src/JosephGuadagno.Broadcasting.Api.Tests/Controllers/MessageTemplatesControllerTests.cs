using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class MessageTemplatesControllerTests
{
    private readonly Mock<IMessageTemplateManager> _messageTemplateManagerMock = new();
    private readonly Mock<ISocialMediaPlatformManager> _socialMediaPlatformManagerMock = new();
    private readonly Mock<IOnboardingManager> _onboardingManagerMock = new();
    private readonly Mock<ILogger<MessageTemplatesController>> _loggerMock = new();

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper Mapper = ApiTestMapper.Instance;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private MessageTemplatesController CreateSut(string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        var controller = new MessageTemplatesController(
            _messageTemplateManagerMock.Object,
            _socialMediaPlatformManagerMock.Object,
            _onboardingManagerMock.Object,
            _loggerMock.Object,
            Mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    private static SocialMediaPlatform BuildPlatform(int id = 1, string name = "TestPlatform")=> new()
    {
        Id = id,
        Name = name,
        IsActive = true
    };

    private static MessageTemplate BuildTemplate(string oid = "owner-oid-12345") => new()
    {
        SocialMediaPlatformId = 1,
        MessageType = "RandomPost",
        Template = "Check out {{ title }}!",
        CreatedByEntraOid = oid
    };

    // -------------------------------------------------------------------------
    // Security: GetAsync — user with no template gets 404 (not Forbid)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenUserHasNoTemplate_ReturnsNotFound()
    {
        // Arrange — the user has no template; 3-arg GetAsync returns null
        var platform = BuildPlatform();

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateManagerMock
            .Setup(m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetAsync("TestPlatform", "RandomPost");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _messageTemplateManagerMock.Verify(
            m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Security: UpdateAsync — user with no template gets 404 (not Forbid)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenUserHasNoTemplate_ReturnsNotFound()
    {
        // Arrange
        var platform = BuildPlatform();
        var request = new MessageTemplateRequest { Template = "Updated {{ title }}!" };

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateManagerMock
            .Setup(m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MessageTemplate?)null);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.UpdateAsync("TestPlatform", "RandomPost", request);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _messageTemplateManagerMock.Verify(
            m => m.UpdateAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // GetDefaultAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDefaultAsync_WhenDefaultExists_ReturnsOk()
    {
        // Arrange
        var platform = BuildPlatform();
        var defaultTemplate = BuildTemplate(oid: ""); // system default

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateManagerMock
            .Setup(m => m.GetAsync(platform.Id, "RandomPost", "", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTemplate);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAsync("TestPlatform", "RandomPost");

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.MessageType.Should().Be("RandomPost");
    }

    [Fact]
    public async Task GetDefaultAsync_WhenPlatformNotFound_ReturnsNotFound()
    {
        // Arrange
        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("UnknownPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.GetDefaultAsync("UnknownPlatform", "RandomPost");

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------------------------------
    // GetAllDefaultsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllDefaultsAsync_ReturnsOkWithDefaults()
    {
        // Arrange
        var defaults = new List<MessageTemplate>
        {
            BuildTemplate(oid: ""),
            new() { SocialMediaPlatformId = 1, MessageType = "NewPost", Template = "{{ title }}", CreatedByEntraOid = "" }
        };
        _messageTemplateManagerMock
            .Setup(m => m.GetAllDefaultsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaults);

        var sut = CreateSut();

        // Act
        var result = await sut.GetAllDefaultsAsync();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var items = okResult.Value.Should().BeAssignableTo<IEnumerable<MessageTemplateResponse>>().Subject;
        items.Should().HaveCount(2);
    }

    // -------------------------------------------------------------------------
    // CreateAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateAsync_WhenValid_ReturnsCreatedAtAction()
    {
        // Arrange
        var platform = BuildPlatform();
        var request = new MessageTemplateRequest { Template = "{{ title }} {{ url }}" };
        var created = BuildTemplate(oid: "owner-oid-12345");

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(created);

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        // Act
        var result = await sut.CreateAsync("TestPlatform", "RandomPost", request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        _messageTemplateManagerMock.Verify(
            m => m.CreateAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenPlatformNotFound_ReturnsNotFound()
    {
        // Arrange
        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("UnknownPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialMediaPlatform?)null);

        var sut = CreateSut();

        // Act
        var result = await sut.CreateAsync("UnknownPlatform", "RandomPost", new MessageTemplateRequest { Template = "x" });

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    // -------------------------------------------------------------------------
    // Security: GetAllAsync — SiteAdmin calls unfiltered overload
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenSiteAdmin_CallsUnfilteredGetAll()
    {
        // Arrange
        var templates = new List<MessageTemplate> { BuildTemplate() };
        // Set up the unfiltered overload (no ownerOid — first param is int page).
        _messageTemplateManagerMock
            .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MessageTemplate> { Items = templates, TotalCount = templates.Count });

        var sut = CreateSut(isSiteAdmin: true);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);

        // Unfiltered overload must be invoked exactly once …
        _messageTemplateManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // … and the owner-filtered overload must never be called.
        _messageTemplateManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // GetAllAsync — non-admin owner-filtered path and guards
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenNotSiteAdmin_CallsOwnerFilteredGetAll()
    {
        // Arrange
        var templates = new List<MessageTemplate> { BuildTemplate() };
        _messageTemplateManagerMock
            .Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MessageTemplate> { Items = templates, TotalCount = templates.Count });

        var sut = CreateSut(ownerOid: "owner-oid-12345", isSiteAdmin: false);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);

        // Owner-filtered overload must fire …
        _messageTemplateManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // … and the unfiltered overload must never be called.
        _messageTemplateManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_WhenPageIsZero_ClampsToDefaultPage()
    {
        // Arrange
        _messageTemplateManagerMock
            .Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MessageTemplate> { Items = new List<MessageTemplate>(), TotalCount = 0 });

        var sut = CreateSut();

        // Act — page = 0 must be clamped to Pagination.DefaultPage
        var result = await sut.GetAllAsync(page: 0);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Page.Should().Be(Pagination.DefaultPage);
    }

    [Fact]
    public async Task GetAllAsync_WhenPageSizeIsZero_ClampsToDefaultPageSize()
    {
        // Arrange
        _messageTemplateManagerMock
            .Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MessageTemplate> { Items = new List<MessageTemplate>(), TotalCount = 0 });

        var sut = CreateSut();

        // Act — pageSize = 0 must be clamped to Pagination.DefaultPageSize
        var result = await sut.GetAllAsync(pageSize: 0);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.PageSize.Should().Be(Pagination.DefaultPageSize);
    }
}
