using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

using System.Security.Claims;

using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Tests.Helpers;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class EngagementsControllerTests
{
    private readonly Mock<IEngagementService> _engagementService;
    private readonly Mock<ISocialMediaPlatformService> _socialMediaPlatformService;
    private readonly Mock<IMapper> _mapper;
    private readonly EngagementsController _controller;

    public EngagementsControllerTests()
    {
        _engagementService = new Mock<IEngagementService>();
        _socialMediaPlatformService = new Mock<ISocialMediaPlatformService>();
        _mapper = new Mock<IMapper>();
        _controller = new EngagementsController(_engagementService.Object, _socialMediaPlatformService.Object, _mapper.Object);
        
        // Initialize TempData
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------


    [Fact]
    public async Task Index_ShouldReturnViewWithEngagementViewModels()
    {
        // Arrange
        var engagements = new List<Engagement> { new Engagement { Id = 1 } };
        var pagedEngagements = new PagedResult<Engagement> { Items = engagements, TotalCount = engagements.Count };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1 } };
        _engagementService.Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Details_WhenEngagementFound_ShouldReturnViewWithEngagementViewModel()
    {
        // Arrange
        var userOid = "test-user-oid";
        var engagement = new Engagement { Id = 1, CreatedByEntraOid = userOid };
        var viewModel = new EngagementViewModel { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.GetPlatformsForEngagementAsync(1)).ReturnsAsync(new List<EngagementSocialMediaPlatform>());
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);
        _mapper.Setup(m => m.Map<List<EngagementSocialMediaPlatformViewModel>>(It.IsAny<object>())).Returns(new List<EngagementSocialMediaPlatformViewModel>());

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task Details_WhenEngagementNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _engagementService.Setup(s => s.GetEngagementAsync(99)).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Details(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WhenEngagementFound_ShouldReturnViewWithEngagementViewModel()
    {
        // Arrange
        var userOid = "test-user-oid";
        var engagement = new Engagement { Id = 1, CreatedByEntraOid = userOid };
        var viewModel = new EngagementViewModel { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.GetPlatformsForEngagementAsync(1))
            .ReturnsAsync(new List<EngagementSocialMediaPlatform>());
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);
        _mapper.Setup(m => m.Map<List<EngagementSocialMediaPlatformViewModel>>(It.IsAny<object>()))
            .Returns(new List<EngagementSocialMediaPlatformViewModel>());

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_WhenEngagementNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _engagementService.Setup(s => s.GetEngagementAsync(99)).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var userOid = "test-user-oid";
        var viewModel = new EngagementViewModel { Id = 1 };
        var existingEngagement = new Engagement { Id = 1, CreatedByEntraOid = userOid };
        var savedEngagement = new Engagement { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(existingEngagement);
        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync(savedEngagement);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveFails_ShouldRedirectBackToEdit()
    {
        // Arrange
        var userOid = "test-user-oid";
        var viewModel = new EngagementViewModel { Id = 5 };
        var existingEngagement = new Engagement { Id = 5, CreatedByEntraOid = userOid };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(5)).ReturnsAsync(existingEngagement);
        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(5, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task Edit_Post_WhenEngagementNotFound_ShouldReturnNotFound()
    {
        // Arrange — issue #742: defence-in-depth fetch returns NotFound
        var viewModel = new EngagementViewModel { Id = 99 };
        _engagementService.Setup(s => s.GetEngagementAsync(99)).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _engagementService.Verify(s => s.SaveEngagementAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — issue #742: ownership re-verification prevents save by non-owner
        var viewModel = new EngagementViewModel { Id = 1 };
        var existingEngagement = new Engagement { Id = 1, CreatedByEntraOid = "owner-oid-12345" };

        // User OID "non-owner-oid-99999" does not match entity's "owner-oid-12345".
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(existingEngagement);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to edit this engagement.", _controller.TempData["ErrorMessage"]);
        _engagementService.Verify(s => s.SaveEngagementAsync(It.IsAny<Engagement>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsSiteAdministrator_ShouldAllowSaveRegardlessOfOwnership()
    {
        // Arrange — issue #742: SiteAdministrators bypass ownership check
        var viewModel = new EngagementViewModel { Id = 1 };
        var existingEngagement = new Engagement { Id = 1, CreatedByEntraOid = "another-user-oid" };
        var savedEngagement = new Engagement { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "admin-oid"),
            new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(existingEngagement);
        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync(savedEngagement);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _engagementService.Verify(s => s.SaveEngagementAsync(It.IsAny<Engagement>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnConfirmationView()
    {
        // Arrange
        var userOid = "test-user-oid";
        var engagement = new Engagement { Id = 1, CreatedByEntraOid = userOid };
        var viewModel = new EngagementViewModel { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteSucceeds_ShouldRedirectToIndex()
    {
        // Arrange
        var engagement = new Engagement { Id = 1, CreatedByEntraOid = "user-oid" };
        
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid"),
            new Claim(ClaimTypes.Role, RoleNames.Administrator)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.DeleteEngagementAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _engagementService.Verify(s => s.DeleteEngagementAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_ShouldReturnView()
    {
        // Arrange
        var engagement = new Engagement { Id = 1, CreatedByEntraOid = "user-oid" };
        var viewModel = new EngagementViewModel { Id = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid"),
            new Claim(ClaimTypes.Role, RoleNames.Administrator)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.DeleteEngagementAsync(1)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Add_Get_ShouldReturnViewWithNewEngagementViewModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EngagementViewModel>(viewResult.Model);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task Add_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var viewModel = new EngagementViewModel { Id = 0 };
        var savedEngagement = new Engagement { Id = 42 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync(savedEngagement);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(42, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task Add_Post_WhenSaveFails_ShouldRedirectToAdd()
    {
        // Arrange
        var viewModel = new EngagementViewModel { Id = 0 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Add", redirectResult.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserIsSiteAdministrator_DeletesAnyEngagement()
    {
        // Arrange
        var engagementId = 1;
        var engagement = new Engagement
        {
            Id = engagementId,
            CreatedByEntraOid = "other-user-oid"
        };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "admin-oid"),
            new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(engagementId)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.DeleteEngagementAsync(engagementId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(engagementId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _engagementService.Verify(s => s.DeleteEngagementAsync(engagementId), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserIsOwner_DeletesOwnEngagement()
    {
        // Arrange
        var engagementId = 1;
        var userOid = "user-oid-12345";
        var engagement = new Engagement
        {
            Id = engagementId,
            CreatedByEntraOid = userOid
        };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementAsync(engagementId)).ReturnsAsync(engagement);
        _engagementService.Setup(s => s.DeleteEngagementAsync(engagementId)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(engagementId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _engagementService.Verify(s => s.DeleteEngagementAsync(engagementId), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserIsNotOwnerAndNotAdmin_RedirectsWithError()
    {
        // Arrange
        var engagementId = 1;
        var engagement = new Engagement
        {
            Id = engagementId,
            CreatedByEntraOid = "owner-oid-12345"
        };

        // User OID "non-owner-oid-99999" does not match entity's "owner-oid-12345".
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();

        _engagementService.Setup(s => s.GetEngagementAsync(engagementId)).ReturnsAsync(engagement);

        // Act
        var result = await _controller.DeleteConfirmed(engagementId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to delete this engagement.", _controller.TempData["ErrorMessage"]);
        _engagementService.Verify(s => s.DeleteEngagementAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Add_Post_SetsCreatedByEntraOid()
    {
        // Arrange
        var userOid = "user-oid-67890";
        var viewModel = new EngagementViewModel { Id = 0, Name = "Test Engagement" };
        var savedEngagement = new Engagement { Id = 42, CreatedByEntraOid = userOid };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        Engagement? capturedEngagement = null;
        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService
            .Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>()))
            .Callback<Engagement>(e => capturedEngagement = e)
            .ReturnsAsync(savedEngagement);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.NotNull(capturedEngagement);
        Assert.Equal(userOid, capturedEngagement!.CreatedByEntraOid);
    }

    [Fact]
    public void EngagementsController_HasRequireViewerPolicy()
    {
        // Arrange & Act
        var controllerType = typeof(EngagementsController);
        var attributes = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), false);

        // Assert
        Assert.NotEmpty(attributes);
        var authorizeAttribute = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("RequireViewer", authorizeAttribute!.Policy);
    }

    [Fact]
    public void GetAdd_Action_HasRequireContributorPolicy()
    {
        // Arrange & Act
        var method = typeof(EngagementsController).GetMethod("Add", Type.EmptyTypes);

        // Assert
        Assert.NotNull(method);
        var attributes = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        Assert.NotEmpty(attributes);
        var authorizeAttribute = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("RequireContributor", authorizeAttribute!.Policy);
    }

    [Fact]
    public void GetEdit_Action_HasRequireContributorPolicy()
    {
        // Arrange & Act
        var method = typeof(EngagementsController).GetMethod("Edit", new[] { typeof(int) });

        // Assert
        Assert.NotNull(method);
        var attributes = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        Assert.NotEmpty(attributes);
        var authorizeAttribute = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("RequireContributor", authorizeAttribute!.Policy);
    }

    [Fact]
    public void GetDelete_Action_HasRequireContributorPolicy()
    {
        // Arrange & Act
        var method = typeof(EngagementsController).GetMethod("Delete", new[] { typeof(int) });

        // Assert
        Assert.NotNull(method);
        var attributes = method!.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        Assert.NotEmpty(attributes);
        var authorizeAttribute = attributes.First() as AuthorizeAttribute;
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("RequireContributor", authorizeAttribute!.Policy);
    }

    #region AddPlatform Tests (Issue #708 Regression Coverage)

    [Fact]
    public async Task AddPlatform_Get_ShouldReturnViewWithViewModel()
    {
        // Arrange
        var engagementId = 42;
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatform { Id = 2, Name = "LinkedIn", IsActive = true }
        };
        _socialMediaPlatformService.Setup(s => s.GetAllAsync(true)).ReturnsAsync(platforms);

        // Act
        var result = await _controller.AddPlatform(engagementId);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<EngagementSocialMediaPlatformViewModel>(viewResult.Model);
        Assert.Equal(engagementId, model.EngagementId);
        Assert.Equal(platforms, _controller.ViewBag.Platforms);
        _socialMediaPlatformService.Verify(s => s.GetAllAsync(true), Times.Once);
    }

    [Fact]
    public async Task AddPlatform_Post_WhenModelStateInvalid_ShouldReturnViewWithPlatforms()
    {
        // Arrange
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 0 // Invalid - fails [Range(1, int.MaxValue)] validation
        };
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }
        };
        _socialMediaPlatformService.Setup(s => s.GetAllAsync(false)).ReturnsAsync(platforms);
        _controller.ModelState.AddModelError("SocialMediaPlatformId", "Please select a platform.");

        // Act
        var result = await _controller.AddPlatform(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        Assert.Equal(platforms, _controller.ViewBag.Platforms);
        _engagementService.Verify(
            s => s.AddPlatformToEngagementAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()),
            Times.Never,
            "Service should not be called when ModelState is invalid");
    }

    [Fact]
    public async Task AddPlatform_Post_WhenValidAndSuccessful_ShouldRedirectWithSuccessMessage()
    {
        // Arrange
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };
        var addedPlatform = new EngagementSocialMediaPlatform
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };
        _engagementService
            .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
            .ReturnsAsync(addedPlatform);

        // Act
        var result = await _controller.AddPlatform(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(42, redirectResult.RouteValues?["id"]);
        Assert.Equal("Platform added successfully.", _controller.TempData["SuccessMessage"]);
        _engagementService.Verify(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"), Times.Once);
    }

    [Fact]
    public async Task AddPlatform_Post_WhenServiceReturnsNull_ShouldRedirectWithErrorMessage()
    {
        // Arrange
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };
        _engagementService
            .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
            .ReturnsAsync((EngagementSocialMediaPlatform?)null);

        // Act
        var result = await _controller.AddPlatform(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(42, redirectResult.RouteValues?["id"]);
        Assert.Equal("Failed to add platform to engagement.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task AddPlatform_Post_When409Conflict_ShouldRedirectWithWarningMessage()
    {
        // Arrange
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };
        var conflictException = new HttpRequestException(
            "Conflict",
            null,
            System.Net.HttpStatusCode.Conflict);
        _engagementService
            .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
            .ThrowsAsync(conflictException);

        // Act
        var result = await _controller.AddPlatform(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(42, redirectResult.RouteValues?["id"]);
        Assert.Equal("This platform is already associated with this engagement.", _controller.TempData["WarningMessage"]);
    }

    [Fact]
    public async Task AddPlatform_Post_WhenNon409HttpRequestException_ShouldRedirectWithErrorMessage()
    {
        // Arrange
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };
        var exceptionMessage = "Bad Request";
        var badRequestException = new HttpRequestException(
            exceptionMessage,
            null,
            System.Net.HttpStatusCode.BadRequest);
        _engagementService
            .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
            .ThrowsAsync(badRequestException);

        // Act
        var result = await _controller.AddPlatform(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(42, redirectResult.RouteValues?["id"]);
        Assert.Contains("Failed to add platform", (string)_controller.TempData["ErrorMessage"]!);
    }

    [Fact]
    public async Task AddPlatform_Post_DuplicateAttempt_ShouldHandleWithWarning()
    {
        // Arrange - Simulates the double-submit scenario from issue #708
        // First call succeeds, second call would fail with 409 Conflict
        var viewModel = new EngagementSocialMediaPlatformViewModel
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };

        var firstCallSucceeds = new EngagementSocialMediaPlatform
        {
            EngagementId = 42,
            SocialMediaPlatformId = 1,
            Handle = "@TestHandle"
        };

        var callCount = 0;
        _engagementService
            .Setup(s => s.AddPlatformToEngagementAsync(42, 1, "@TestHandle"))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return firstCallSucceeds;
                }
                throw new HttpRequestException("Conflict", null, System.Net.HttpStatusCode.Conflict);
            });

        // Act - First call (succeeds)
        var firstResult = await _controller.AddPlatform(viewModel);

        // Assert first call
        var firstRedirect = Assert.IsType<RedirectToActionResult>(firstResult);
        Assert.Equal("Edit", firstRedirect.ActionName);
        Assert.Equal("Platform added successfully.", _controller.TempData["SuccessMessage"]);

        // Act - Second call (simulates double-submit or retry after error)
        var secondResult = await _controller.AddPlatform(viewModel);

        // Assert second call - Conflict should show warning, not error
        var secondRedirect = Assert.IsType<RedirectToActionResult>(secondResult);
        Assert.Equal("Edit", secondRedirect.ActionName);
        Assert.Equal("This platform is already associated with this engagement.", _controller.TempData["WarningMessage"]);
    }

    #endregion

    #region RemovePlatform Tests

    [Fact]
    public async Task RemovePlatform_WhenSuccessful_ShouldRedirectWithSuccessMessage()
    {
        // Arrange
        var engagementId = 42;
        var platformId = 1;
        _engagementService
            .Setup(s => s.RemovePlatformFromEngagementAsync(engagementId, platformId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemovePlatform(engagementId, platformId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(engagementId, redirectResult.RouteValues?["id"]);
        Assert.Equal("Platform removed successfully.", _controller.TempData["SuccessMessage"]);
        _engagementService.Verify(s => s.RemovePlatformFromEngagementAsync(engagementId, platformId), Times.Once);
    }

    [Fact]
    public async Task RemovePlatform_WhenFails_ShouldRedirectWithErrorMessage()
    {
        // Arrange
        var engagementId = 42;
        var platformId = 1;
        _engagementService
            .Setup(s => s.RemovePlatformFromEngagementAsync(engagementId, platformId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.RemovePlatform(engagementId, platformId);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(engagementId, redirectResult.RouteValues?["id"]);
        Assert.Equal("Failed to remove platform from engagement.", _controller.TempData["ErrorMessage"]);
    }

    #endregion

    #region Sort and Filter Tests

    [Fact]
    public async Task Index_WithNoParameters_CallsServiceWithDefaults()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1 } },
            TotalCount = 1
        };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1 } };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "startdate", true, null))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        _engagementService.Verify(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "startdate", true, null), Times.Once);
    }

    [Fact]
    public async Task Index_WithSortByName_CallsServiceWithNameSort()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Alpha" } },
            TotalCount = 1
        };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1, Name = "Alpha" } };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "name", false, null))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index(sortBy: "name", sortDescending: false);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        _engagementService.Verify(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "name", false, null), Times.Once);
    }

    [Fact]
    public async Task Index_WithFilter_CallsServiceWithFilter()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Tech Conference" } },
            TotalCount = 1
        };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1, Name = "Tech Conference" } };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "startdate", true, "test"))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index(filter: "test");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        _engagementService.Verify(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "startdate", true, "test"), Times.Once);
    }

    [Fact]
    public async Task Index_SetsViewBagSortBy()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement>(),
            TotalCount = 0
        };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), "name", It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(new List<EngagementViewModel>());

        // Act
        var result = await _controller.Index(sortBy: "name");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("name", _controller.ViewBag.SortBy);
    }

    [Fact]
    public async Task Index_SetsViewBagSortDescending()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement>(),
            TotalCount = 0
        };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), false, It.IsAny<string?>()))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(new List<EngagementViewModel>());

        // Act
        var result = await _controller.Index(sortDescending: false);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(false, _controller.ViewBag.SortDescending);
    }

    [Fact]
    public async Task Index_SetsViewBagFilter()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement>(),
            TotalCount = 0
        };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), "conference"))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(new List<EngagementViewModel>());

        // Act
        var result = await _controller.Index(filter: "conference");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("conference", _controller.ViewBag.Filter);
    }

    [Fact]
    public async Task Index_WithAllParameters_CallsServiceAndSetsViewBag()
    {
        // Arrange
        var pagedEngagements = new PagedResult<Engagement> 
        { 
            Items = new List<Engagement> { new Engagement { Id = 1, Name = "Code Conference" } },
            TotalCount = 1
        };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1, Name = "Code Conference" } };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(2, It.IsAny<int?>(), "enddate", false, "code"))
            .ReturnsAsync(pagedEngagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index(page: 2, sortBy: "enddate", sortDescending: false, filter: "code");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("enddate", _controller.ViewBag.SortBy);
        Assert.Equal(false, _controller.ViewBag.SortDescending);
        Assert.Equal("code", _controller.ViewBag.Filter);
        _engagementService.Verify(s => s.GetEngagementsAsync(2, It.IsAny<int?>(), "enddate", false, "code"), Times.Once);
    }

    #endregion

    #region GetCalendarEvents Tests (Issue #741)

    [Fact]
    public async Task GetCalendarEvents_WhenEngagementsExist_ShouldReturnJsonCalendarEvents()
    {
        // Arrange — issue #741: calendar events are filtered transparently by the API via bearer token
        var startTime = new DateTimeOffset(2025, 6, 1, 9, 0, 0, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2025, 6, 1, 17, 0, 0, TimeSpan.Zero);
        var engagements = new List<Engagement>
        {
            new Engagement { Id = 1, Name = "Tech Summit", StartDateTime = startTime, EndDateTime = endTime, Url = "https://example.com" }
        };
        var pagedResult = new PagedResult<Engagement> { Items = engagements, TotalCount = 1 };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetCalendarEvents();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        _engagementService.Verify(
            s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Once);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task GetCalendarEvents_WhenNoEngagements_ShouldReturnEmptyJsonArray()
    {
        // Arrange
        var emptyResult = new PagedResult<Engagement> { Items = new List<Engagement>(), TotalCount = 0 };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(emptyResult);

        // Act
        var result = await _controller.GetCalendarEvents();

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        var emptyArray = Assert.IsAssignableFrom<object[]>(jsonResult.Value);
        Assert.Empty(emptyArray);
    }

    #endregion

    #region Index Filtering Tests (Issue #741)

    [Fact]
    public async Task Index_FilteringIsDelegatedToService_ServiceCalledForAuthenticatedUser()
    {
        // Arrange — issue #741: the Web controller delegates filtering to the service (which calls the API).
        // The API transparently filters by the caller's bearer token OID, so no explicit ownerOid is
        // passed from the Web controller — per-user isolation is enforced at the API layer.
        var userOid = "contributor-oid";
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, userOid),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var pagedResult = new PagedResult<Engagement> { Items = new List<Engagement>(), TotalCount = 0 };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(pagedResult);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(new List<EngagementViewModel>());

        // Act
        var result = await _controller.Index();

        // Assert — service was called once; API layer applies per-user OID filter via bearer token
        Assert.IsType<ViewResult>(result);
        _engagementService.Verify(
            s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task Index_ForSiteAdministrator_ServiceIsCalledAndCanReturnAllRecords()
    {
        // Arrange — issue #741: SiteAdministrators see all records (API returns unfiltered for admins)
        var adminOid = "admin-oid";
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, adminOid),
            new Claim(ClaimTypes.Role, RoleNames.SiteAdministrator)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        var allEngagements = new List<Engagement>
        {
            new Engagement { Id = 1, CreatedByEntraOid = "user-a" },
            new Engagement { Id = 2, CreatedByEntraOid = "user-b" }
        };
        var pagedResult = new PagedResult<Engagement> { Items = allEngagements, TotalCount = 2 };
        _engagementService
            .Setup(s => s.GetEngagementsAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(pagedResult);
        _mapper
            .Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>()))
            .Returns(new List<EngagementViewModel> { new EngagementViewModel { Id = 1 }, new EngagementViewModel { Id = 2 } });

        // Act
        var result = await _controller.Index();

        // Assert — admin receives engagements from all users (API returns unfiltered set)
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<EngagementViewModel>>(viewResult.Model);
        Assert.Equal(2, model.Count);
    }

    #endregion
}
