using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class PublisherSettingsControllerTests
{
    private readonly Mock<IUserPublisherSettingService> _publisherSettingService = new();
    private readonly Mock<ISocialMediaPlatformService> _platformService = new();
    private readonly Mock<IUserApprovalManager> _userApprovalManager = new();
    private readonly Mock<ILogger<PublisherSettingsController>> _logger = new();
    private readonly PublisherSettingsController _sut;

    public PublisherSettingsControllerTests()
    {
        _sut = new PublisherSettingsController(
            _publisherSettingService.Object,
            _platformService.Object,
            _userApprovalManager.Object,
            _logger.Object);

        _sut.TempData = new TempDataDictionary(
            new DefaultHttpContext(),
            Mock.Of<ITempDataProvider>());
    }

    [Fact]
    public async Task Index_ShouldReturnViewForCurrentUser()
    {
        // Arrange
        SetUser("current-user-oid", RoleNames.Contributor);
        _userApprovalManager
            .Setup(manager => manager.GetUserAsync("current-user-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 7,
                EntraObjectId = "current-user-oid",
                DisplayName = "Switch",
                ApprovalStatus = "Approved",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        _platformService
            .Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), true))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items =
                [
                    new SocialMediaPlatform { Id = 1, Name = "BlueSky", Icon = "bi-cloud", IsActive = true },
                    new SocialMediaPlatform { Id = 99, Name = "Mastodon", Icon = "bi-mastodon", IsActive = true }
                ],
                TotalCount = 2
            });

        _publisherSettingService
            .Setup(service => service.GetCurrentUserAsync())
            .ReturnsAsync(
            [
                new UserPublisherSetting
                {
                    Id = 5,
                    CreatedByEntraOid = "current-user-oid",
                    SocialMediaPlatformId = 1,
                    IsEnabled = true,
                    Settings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        [nameof(BlueskyPublisherSettings.BlueskyUserName)] = "@switch",
                        [nameof(BlueskyPublisherSettings.BlueskyPassword)] = "••••••••"
                    },
                    WriteOnlyFields = [nameof(BlueskyPublisherSettings.BlueskyPassword)]
                }
            ]);

        // Act
        var result = await _sut.Index();

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        var model = viewResult.Model.Should().BeOfType<UserPublisherSettingsPageViewModel>().Subject;
        model.TargetUserEntraOid.Should().Be("current-user-oid");
        model.Platforms.Should().HaveCount(2);
        model.Platforms[0].Should().BeOfType<BlueskyPublisherSettingsViewModel>();
        model.Platforms[1].Should().BeOfType<UnsupportedPublisherSettingsViewModel>();
    }

    [Fact]
    public async Task Index_WhenManagingAnotherUserWithoutSiteAdminRole_ShouldRedirectBackToOwnSettings()
    {
        // Arrange
        SetUser("current-user-oid", RoleNames.Contributor);
        _userApprovalManager
            .Setup(manager => manager.GetUserAsync("current-user-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 1,
                EntraObjectId = "current-user-oid",
                DisplayName = "Current User",
                ApprovalStatus = "Approved",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        // Act
        var result = await _sut.Index("other-user-oid");

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PublisherSettingsController.Index));
        _sut.TempData["ErrorMessage"].Should().Be("Only Site Administrators can manage another user's publisher settings.");
    }

    [Fact]
    public async Task SaveBluesky_WhenModelIsInvalid_ShouldRebuildIndexView()
    {
        // Arrange
        SetUser("current-user-oid", RoleNames.Contributor);
        _userApprovalManager
            .Setup(manager => manager.GetUserAsync("current-user-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 1,
                EntraObjectId = "current-user-oid",
                DisplayName = "Current User",
                ApprovalStatus = "Approved",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        _platformService
            .Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), true))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items = [new SocialMediaPlatform { Id = 2, Name = "BlueSky", IsActive = true }],
                TotalCount = 1
            });

        _publisherSettingService
            .Setup(service => service.GetCurrentUserAsync())
            .ReturnsAsync([]);

        var model = new BlueskyPublisherSettingsViewModel
        {
            CreatedByEntraOid = "current-user-oid",
            SocialMediaPlatformId = 2,
            PlatformName = "BlueSky",
            IsEnabled = true
        };

        _sut.ModelState.AddModelError(nameof(BlueskyPublisherSettingsViewModel.AppPassword), "Required");

        // Act
        var result = await _sut.SaveBluesky(model);

        // Assert
        var viewResult = result.Should().BeOfType<ViewResult>().Subject;
        viewResult.ViewName.Should().Be("Index");
        _publisherSettingService.Verify(service => service.SaveCurrentUserAsync(It.IsAny<UserPublisherSetting>()), Times.Never);
    }

    [Fact]
    public async Task SaveLinkedIn_WhenValid_ShouldPersistAndRedirect()
    {
        // Arrange
        SetUser("current-user-oid", RoleNames.SiteAdministrator);

        _userApprovalManager
            .Setup(manager => manager.GetUserAsync("target-user-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ApplicationUser
            {
                Id = 2,
                EntraObjectId = "target-user-oid",
                DisplayName = "Target User",
                ApprovalStatus = "Approved",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

        _publisherSettingService
            .Setup(service => service.SaveByUserAsync("target-user-oid", It.IsAny<UserPublisherSetting>()))
            .ReturnsAsync(new UserPublisherSetting
            {
                Id = 12,
                CreatedByEntraOid = "target-user-oid",
                SocialMediaPlatformId = 3,
                IsEnabled = true
            });

        var model = new LinkedInPublisherSettingsViewModel
        {
            CreatedByEntraOid = "target-user-oid",
            SocialMediaPlatformId = 3,
            PlatformName = "LinkedIn",
            IsEnabled = true,
            AuthorId = "author-1",
            ClientId = "client-1",
            ClientSecret = "secret",
            AccessToken = "token"
        };

        // Act
        var result = await _sut.SaveLinkedIn(model);

        // Assert
        var redirect = result.Should().BeOfType<RedirectToActionResult>().Subject;
        redirect.ActionName.Should().Be(nameof(PublisherSettingsController.Index));
        redirect.RouteValues.Should().ContainKey("userOid");
        redirect.RouteValues!["userOid"].Should().Be("target-user-oid");
        _publisherSettingService.Verify(service => service.SaveByUserAsync("target-user-oid", It.Is<UserPublisherSetting>(request =>
            request.CreatedByEntraOid == "target-user-oid" &&
            request.Settings[nameof(LinkedInPublisherSettings.AuthorId)] == "author-1" &&
            request.Settings[nameof(LinkedInPublisherSettings.ClientId)] == "client-1")), Times.Once);
    }

    private void SetUser(string entraOid, string role)
    {
        var claims = new List<Claim>
        {
            new(ApplicationClaimTypes.EntraObjectId, entraOid),
            new(ClaimTypes.Role, role)
        };

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }
}
