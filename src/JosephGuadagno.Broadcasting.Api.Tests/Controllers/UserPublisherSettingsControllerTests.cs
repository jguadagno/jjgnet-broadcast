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

public class UserPublisherSettingsControllerTests
{
    private readonly Mock<IUserPublisherSettingManager> _manager = new();
    private readonly Mock<ILogger<UserPublisherSettingsController>> _logger = new();
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    [Fact]
    public async Task GetAllAsync_ShouldUseCurrentUserWhenOwnerQueryMissing()
    {
        _manager
            .Setup(manager => manager.GetByUserAsync("current-user-oid", It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut(Domain.Scopes.UserPublisherSettings.List, "current-user-oid");

        var result = await sut.GetAllAsync();

        result.Result.Should().BeOfType<OkObjectResult>();
        _manager.Verify(manager => manager.GetByUserAsync("current-user-oid", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenTargetingAnotherUserWithoutAdminRole_ShouldReturnForbid()
    {
        var sut = CreateSut(Domain.Scopes.UserPublisherSettings.List, "current-user-oid");

        var result = await sut.GetAllAsync("other-user-oid");

        result.Result.Should().BeOfType<ForbidResult>();
        _manager.Verify(manager => manager.GetByUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_ShouldRespectOwnerQueryForSiteAdministrator()
    {
        _manager
            .Setup(manager => manager.SaveAsync(It.IsAny<UserPublisherSettingUpdate>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserPublisherSetting
            {
                Id = 12,
                CreatedByEntraOid = "target-user-oid",
                SocialMediaPlatformId = 3,
                SocialMediaPlatform = new SocialMediaPlatform { Id = 3, Name = "LinkedIn", IsActive = true },
                IsEnabled = true,
                LinkedIn = new LinkedInPublisherSetting
                {
                    AuthorId = "author-1",
                    ClientId = "client-1",
                    HasClientSecret = true,
                    HasAccessToken = true
                }
            });

        var sut = CreateSut(Domain.Scopes.UserPublisherSettings.Modify, "admin-user-oid", isSiteAdministrator: true);

        var result = await sut.SaveAsync(3, "target-user-oid", new UserPublisherSettingRequest
        {
            IsEnabled = true,
            LinkedIn = new LinkedInPublisherSettingRequest
            {
                AuthorId = "author-1",
                ClientId = "client-1",
                ClientSecret = "secret",
                AccessToken = "token"
            }
        });

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<UserPublisherSettingResponse>().Subject;
        response.CreatedByEntraOid.Should().Be("target-user-oid");
        response.LinkedIn!.HasClientSecret.Should().BeTrue();
        response.LinkedIn.HasAccessToken.Should().BeTrue();
        _manager.Verify(
            manager => manager.SaveAsync(
                It.Is<UserPublisherSettingUpdate>(setting =>
                    setting.CreatedByEntraOid == "target-user-oid"
                    && setting.SocialMediaPlatformId == 3
                    && setting.LinkedIn != null
                    && setting.LinkedIn.AuthorId == "author-1"
                    && setting.LinkedIn.ClientId == "client-1"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private UserPublisherSettingsController CreateSut(string scopeClaimValue, string ownerOid, bool isSiteAdministrator = false)
    {
        return new UserPublisherSettingsController(_manager.Object, _logger.Object, _mapper)
        {
            ControllerContext = CreateControllerContext(scopeClaimValue, ownerOid, isSiteAdministrator),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    private static ControllerContext CreateControllerContext(string scopeClaimValue, string ownerOid, bool isSiteAdministrator)
    {
        var claims = new List<Claim>
        {
            new("scp", scopeClaimValue),
            new("http://schemas.microsoft.com/identity/claims/scope", scopeClaimValue),
            new(ApplicationClaimTypes.EntraObjectId, ownerOid)
        };

        if (isSiteAdministrator)
        {
            claims.Add(new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator));
        }

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
        return new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
    }
}
