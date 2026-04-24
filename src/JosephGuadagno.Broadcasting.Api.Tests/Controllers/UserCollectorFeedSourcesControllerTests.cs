using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

/// <summary>
/// Tests for UserCollectorFeedSourcesController - ownership enforcement and security
/// </summary>
public class UserCollectorFeedSourcesControllerTests
{
    private readonly Mock<IUserCollectorFeedSourceManager> _manager = new();
    private readonly Mock<ILogger<UserCollectorFeedSourcesController>> _logger = new();
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    [Fact]
    public async Task GetAllAsync_ReturnsCurrentUserConfigs_WhenOwnerQueryMissing()
    {
        // Arrange
        const string currentUserOid = "current-user-oid-11111111";
        _manager
            .Setup(m => m.GetByUserAsync(currentUserOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut(currentUserOid);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _manager.Verify(m => m.GetByUserAsync(currentUserOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsForbid_WhenNonAdminTargetsAnotherUser()
    {
        // Arrange
        const string currentUserOid = "current-user-oid-11111111";
        const string targetUserOid = "target-user-oid-22222222";

        var sut = CreateSut(currentUserOid, isSiteAdmin: false);

        // Act
        var result = await sut.GetAllAsync(targetUserOid);

        // Assert - non-admin cannot query another user's configs
        result.Result.Should().BeOfType<ForbidResult>();
        _manager.Verify(m => m.GetByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsTargetUserConfigs_WhenSiteAdminTargetsAnotherUser()
    {
        // Arrange
        const string adminUserOid = "admin-user-oid-11111111";
        const string targetUserOid = "target-user-oid-22222222";
        var targetConfigs = new List<UserCollectorFeedSource>
        {
            BuildFeedSource(1, targetUserOid, "https://target.com/feed.xml")
        };

        _manager
            .Setup(m => m.GetByUserAsync(targetUserOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(targetConfigs);

        var sut = CreateSut(adminUserOid, isSiteAdmin: true);

        // Act
        var result = await sut.GetAllAsync(targetUserOid);

        // Assert - admin can query another user's configs
        result.Result.Should().BeOfType<OkObjectResult>();
        _manager.Verify(m => m.GetByUserAsync(targetUserOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsForbid_WhenCallerIsNotOwnerAndNotAdmin()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        const string nonOwnerOid = "non-owner-oid-22222222";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sut = CreateSut(nonOwnerOid, isSiteAdmin: false);

        // Act
        var result = await sut.GetByIdAsync(5);

        // Assert - non-owner cannot read another user's config
        result.Result.Should().BeOfType<ForbidResult>();
        _manager.Verify(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsConfig_WhenCallerIsOwner()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sut = CreateSut(ownerOid);

        // Act
        var result = await sut.GetByIdAsync(5);

        // Assert - owner can read their own config
        result.Result.Should().BeOfType<OkObjectResult>();
        _manager.Verify(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsConfig_WhenCallerIsSiteAdmin()
    {
        // Arrange
        const string adminOid = "admin-oid-11111111";
        const string ownerOid = "owner-oid-22222222";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sut = CreateSut(adminOid, isSiteAdmin: true);

        // Act
        var result = await sut.GetByIdAsync(5);

        // Assert - admin can read any user's config
        result.Result.Should().BeOfType<OkObjectResult>();
        _manager.Verify(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostAsync_SetsCreatedByEntraOidFromCurrentUser_NotRequestBody()
    {
        // Arrange
        const string currentUserOid = "current-user-oid-11111111";
        var request = new UserCollectorFeedSourceRequest
        {
            FeedUrl = "https://example.com/feed.xml",
            DisplayName = "New Feed",
            IsActive = true
        };

        UserCollectorFeedSource? capturedConfig = null;
        _manager
            .Setup(m => m.SaveAsync(It.IsAny<UserCollectorFeedSource>(), It.IsAny<CancellationToken>()))
            .Callback<UserCollectorFeedSource, CancellationToken>((config, _) => capturedConfig = config)
            .ReturnsAsync((UserCollectorFeedSource config, CancellationToken _) => config with { Id = 10 });

        var sut = CreateSut(currentUserOid);

        // Act
        var result = await sut.PostAsync(request);

        // Assert - CreatedByEntraOid MUST come from the authenticated user, never the request body
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        capturedConfig.Should().NotBeNull();
        capturedConfig!.CreatedByEntraOid.Should().Be(currentUserOid);
        capturedConfig.FeedUrl.Should().Be("https://example.com/feed.xml");
        capturedConfig.DisplayName.Should().Be("New Feed");
    }

    [Fact]
    public async Task PutAsync_ReturnsForbid_WhenNonOwnerAttemptsUpdate()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        const string nonOwnerOid = "non-owner-oid-22222222";
        var existingConfig = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        var request = new UserCollectorFeedSourceRequest
        {
            FeedUrl = "https://malicious.com/feed.xml",
            DisplayName = "Hacked Feed",
            IsActive = true
        };

        var sut = CreateSut(nonOwnerOid, isSiteAdmin: false);

        // Act
        var result = await sut.PutAsync(5, request);

        // Assert - non-owner cannot update another user's config
        result.Result.Should().BeOfType<ForbidResult>();
        _manager.Verify(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _manager.Verify(m => m.SaveAsync(It.IsAny<UserCollectorFeedSource>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PutAsync_Succeeds_WhenOwnerUpdatesOwnConfig()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        var existingConfig = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingConfig);

        UserCollectorFeedSource? capturedConfig = null;
        _manager
            .Setup(m => m.SaveAsync(It.IsAny<UserCollectorFeedSource>(), It.IsAny<CancellationToken>()))
            .Callback<UserCollectorFeedSource, CancellationToken>((config, _) => capturedConfig = config)
            .ReturnsAsync((UserCollectorFeedSource config, CancellationToken _) => config);

        var request = new UserCollectorFeedSourceRequest
        {
            FeedUrl = "https://owner.com/updated-feed.xml",
            DisplayName = "Updated Feed",
            IsActive = false
        };

        var sut = CreateSut(ownerOid);

        // Act
        var result = await sut.PutAsync(5, request);

        // Assert - owner can update their own config
        result.Result.Should().BeOfType<OkObjectResult>();
        capturedConfig.Should().NotBeNull();
        capturedConfig!.CreatedByEntraOid.Should().Be(ownerOid);
        capturedConfig.FeedUrl.Should().Be("https://owner.com/updated-feed.xml");
        capturedConfig.DisplayName.Should().Be("Updated Feed");
        _manager.Verify(m => m.SaveAsync(It.IsAny<UserCollectorFeedSource>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsForbid_WhenNonOwnerAttemptsDelete()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        const string nonOwnerOid = "non-owner-oid-22222222";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var sut = CreateSut(nonOwnerOid, isSiteAdmin: false);

        // Act
        var result = await sut.DeleteAsync(5);

        // Assert - non-owner cannot delete another user's config
        result.Should().BeOfType<ForbidResult>();
        _manager.Verify(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        _manager.Verify(m => m.DeleteAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenCallerIsOwner()
    {
        // Arrange
        const string ownerOid = "owner-oid-11111111";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _manager
            .Setup(m => m.DeleteAsync(5, ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(ownerOid);

        // Act
        var result = await sut.DeleteAsync(5);

        // Assert - owner can delete their own config
        result.Should().BeOfType<NoContentResult>();
        _manager.Verify(m => m.DeleteAsync(5, ownerOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenCallerIsSiteAdmin()
    {
        // Arrange
        const string adminOid = "admin-oid-11111111";
        const string ownerOid = "owner-oid-22222222";
        var config = BuildFeedSource(5, ownerOid, "https://owner.com/feed.xml");

        _manager
            .Setup(m => m.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _manager
            .Setup(m => m.DeleteAsync(5, ownerOid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut(adminOid, isSiteAdmin: true);

        // Act
        var result = await sut.DeleteAsync(5);

        // Assert - admin can delete any user's config
        result.Should().BeOfType<NoContentResult>();
        _manager.Verify(m => m.DeleteAsync(5, ownerOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    private UserCollectorFeedSourcesController CreateSut(string ownerOid, bool isSiteAdmin = false)
    {
        return new UserCollectorFeedSourcesController(_manager.Object, _logger.Object, _mapper)
        {
            ControllerContext = CreateControllerContext(ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    private static ControllerContext CreateControllerContext(string ownerOid, bool isSiteAdmin)
    {
        var claims = new List<Claim>
        {
            new(ApplicationClaimTypes.EntraObjectId, ownerOid)
        };

        if (isSiteAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
    }

    private static UserCollectorFeedSource BuildFeedSource(int id, string ownerOid, string feedUrl) => new()
    {
        Id = id,
        CreatedByEntraOid = ownerOid,
        FeedUrl = feedUrl,
        DisplayName = "Test Feed",
        IsActive = true,
        CreatedOn = DateTimeOffset.UtcNow,
        LastUpdatedOn = DateTimeOffset.UtcNow
    };
}
