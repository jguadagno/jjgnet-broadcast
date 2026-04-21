using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class MessageTemplatesControllerTests
{
    private readonly Mock<IMessageTemplateDataStore> _messageTemplateDataStoreMock;
    private readonly Mock<ISocialMediaPlatformManager> _socialMediaPlatformManagerMock;
    private readonly Mock<ILogger<MessageTemplatesController>> _loggerMock;

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public MessageTemplatesControllerTests()
    {
        _messageTemplateDataStoreMock = new Mock<IMessageTemplateDataStore>();
        _socialMediaPlatformManagerMock = new Mock<ISocialMediaPlatformManager>();
        _loggerMock = new Mock<ILogger<MessageTemplatesController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private MessageTemplatesController CreateSut(string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        var controller = new MessageTemplatesController(
            _messageTemplateDataStoreMock.Object,
            _socialMediaPlatformManagerMock.Object,
            _loggerMock.Object,
            _mapper)
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
    // Security: GetAsync ΓÇö non-owner returns 403
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Template is owned by "owner-oid-12345"; the calling user has a different OID.
        var platform = BuildPlatform();
        var template = BuildTemplate(oid: "owner-oid-12345");

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateDataStoreMock
            .Setup(m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetAsync("TestPlatform", "RandomPost");

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _messageTemplateDataStoreMock.Verify(
            m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // Security: UpdateAsync ΓÇö non-owner returns 403
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        var platform = BuildPlatform();
        var template = BuildTemplate(oid: "owner-oid-12345");
        var request = new MessageTemplateRequest { Template = "Updated {{ title }}!" };

        _socialMediaPlatformManagerMock
            .Setup(m => m.GetByNameAsync("TestPlatform", It.IsAny<CancellationToken>()))
            .ReturnsAsync(platform);
        _messageTemplateDataStoreMock
            .Setup(m => m.GetAsync(platform.Id, "RandomPost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);

        var sut = CreateSut(ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.UpdateAsync("TestPlatform", "RandomPost", request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _messageTemplateDataStoreMock.Verify(
            m => m.UpdateAsync(It.IsAny<MessageTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // Security: GetAllAsync ΓÇö SiteAdmin calls unfiltered overload
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAllAsync_WhenSiteAdmin_CallsUnfilteredGetAll()
    {
        // Arrange
        var templates = new List<MessageTemplate> { BuildTemplate() };
        // Set up the unfiltered overload (no ownerOid ΓÇö first param is int page).
        _messageTemplateDataStoreMock
            .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<MessageTemplate> { Items = templates, TotalCount = templates.Count });

        var sut = CreateSut(isSiteAdmin: true);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);

        // Unfiltered overload must be invoked exactly once ΓÇª
        _messageTemplateDataStoreMock.Verify(
            m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
        // ΓÇª and the owner-filtered overload must never be called.
        _messageTemplateDataStoreMock.Verify(
            m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
