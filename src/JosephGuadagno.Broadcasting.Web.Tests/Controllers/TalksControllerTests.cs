using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

using System.Security.Claims;

using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class TalksControllerTests
{
    private readonly Mock<IEngagementService> _engagementService;
    private readonly Mock<IMapper> _mapper;
    private readonly TalksController _controller;

    public TalksControllerTests()
    {
        _engagementService = new Mock<IEngagementService>();
        _mapper = new Mock<IMapper>();
        _controller = new TalksController(_engagementService.Object, _mapper.Object);
        
        // Initialize TempData
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a <see cref="ControllerContext"/> whose <see cref="ClaimsPrincipal"/>
    /// carries the given <paramref name="ownerOid"/> and optional <paramref name="role"/>.
    /// </summary>
    private static ControllerContext CreateControllerContext(string ownerOid, string role = RoleNames.Contributor)
    {
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, ownerOid),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    /// <summary>
    /// Creates a controller context where the user OID does NOT match the entity's
    /// <c>CreatedByEntraOid</c>.  Use for testing ownership rejection scenarios
    /// (Web MVC redirects with an error message rather than returning ForbidResult).
    /// </summary>
    private static ControllerContext CreateNonOwnerControllerContext(string role = RoleNames.Contributor) =>
        CreateControllerContext(ownerOid: "non-owner-oid-99999", role: role);

    [Fact]
    public async Task Details_WhenTalkFound_ShouldReturnViewWithTalkViewModel()
    {
        // Arrange
        var talk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "test-oid" };
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, "test-oid") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(talk);
        _mapper.Setup(m => m.Map<TalkViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Details(1, 10);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementTalkAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task Details_WhenTalkNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 99)).ReturnsAsync((Talk?)null);

        // Act
        var result = await _controller.Details(1, 99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WhenTalkFound_ShouldReturnViewWithTalkViewModel()
    {
        // Arrange
        var talk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "test-oid" };
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, "test-oid") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(talk);
        _mapper.Setup(m => m.Map<TalkViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Edit(1, 10);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementTalkAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_WhenTalkNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 99)).ReturnsAsync((Talk?)null);

        // Act
        var result = await _controller.Edit(1, 99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var userOid = "test-user-oid";
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };
        var existingTalk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = userOid };
        var savedTalk = new Talk { Id = 10, EngagementId = 1 };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(existingTalk);
        _mapper.Setup(m => m.Map<Talk>(It.IsAny<object>())).Returns(new Talk());
        _engagementService.Setup(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>())).ReturnsAsync(savedTalk);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(1, redirectResult.RouteValues?["engagementId"]);
        Assert.Equal(10, redirectResult.RouteValues?["talkId"]);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveFails_ShouldRedirectBackToEdit()
    {
        // Arrange
        var userOid = "test-user-oid";
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };
        var existingTalk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = userOid };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(existingTalk);
        _mapper.Setup(m => m.Map<Talk>(It.IsAny<object>())).Returns(new Talk());
        _engagementService.Setup(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>())).ReturnsAsync((Talk?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(1, redirectResult.RouteValues?["engagementId"]);
        Assert.Equal(10, redirectResult.RouteValues?["talkId"]);
    }

    [Fact]
    public async Task Edit_Post_WhenTalkNotFound_ShouldReturnNotFound()
    {
        // Arrange — issue #742: defence-in-depth fetch returns NotFound
        var viewModel = new TalkViewModel { Id = 99, EngagementId = 1 };
        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 99)).ReturnsAsync((Talk?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _engagementService.Verify(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — issue #742: ownership re-verification prevents save by non-owner
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };
        // Entity is owned by "owner-oid-12345"; caller has a different OID — ownership check must reject it.
        var existingTalk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "owner-oid-12345" };

        _controller.ControllerContext = CreateNonOwnerControllerContext();

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(existingTalk);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal("Engagements", redirectResult.ControllerName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
        Assert.Equal("You do not have permission to edit this talk.", _controller.TempData["ErrorMessage"]);
        _engagementService.Verify(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsSiteAdministrator_ShouldAllowSaveRegardlessOfOwnership()
    {
        // Arrange — issue #742: SiteAdministrators bypass ownership check
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };
        var existingTalk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "another-user-oid" };
        var savedTalk = new Talk { Id = 10, EngagementId = 1 };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(existingTalk);
        _mapper.Setup(m => m.Map<Talk>(It.IsAny<object>())).Returns(new Talk());
        _engagementService.Setup(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>())).ReturnsAsync(savedTalk);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _engagementService.Verify(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_WhenUserIsAuthorized_ShouldReturnConfirmationView()
    {
        // Arrange
        var talk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "user-oid" };
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(talk);
        _mapper.Setup(m => m.Map<TalkViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Delete(1, 10);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteSucceeds_ShouldRedirectToEngagementsEdit()
    {
        // Arrange
        var talk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "user-oid" };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(talk);
        _engagementService.Setup(s => s.DeleteEngagementTalkAsync(1, 10)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(1, 10);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal("Engagements", redirectResult.ControllerName);
        Assert.Equal(1, redirectResult.RouteValues?["id"]);
        _engagementService.Verify(s => s.DeleteEngagementTalkAsync(1, 10), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_ShouldReturnView()
    {
        // Arrange
        var talk = new Talk { Id = 10, EngagementId = 1, CreatedByEntraOid = "user-oid" };
        var viewModel = new TalkViewModel { Id = 10, EngagementId = 1 };

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

        _engagementService.Setup(s => s.GetEngagementTalkAsync(1, 10)).ReturnsAsync(talk);
        _engagementService.Setup(s => s.DeleteEngagementTalkAsync(1, 10)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<TalkViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.DeleteConfirmed(1, 10);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Add_Get_ShouldReturnViewWithNewTalkViewModelForEngagement()
    {
        // Act
        var result = _controller.Add(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<TalkViewModel>(viewResult.Model);
        Assert.Equal(1, model.EngagementId);
    }

    [Fact]
    public async Task Add_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var viewModel = new TalkViewModel { Id = 0, EngagementId = 1 };
        var savedTalk = new Talk { Id = 10, EngagementId = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<Talk>(It.IsAny<object>())).Returns(new Talk());
        _engagementService.Setup(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>())).ReturnsAsync(savedTalk);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(1, redirectResult.RouteValues?["engagementId"]);
        Assert.Equal(10, redirectResult.RouteValues?["talkId"]);
    }

    [Fact]
    public async Task Add_Post_WhenSaveFails_ShouldRedirectToAdd()
    {
        // Arrange
        var viewModel = new TalkViewModel { Id = 0, EngagementId = 1 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<Talk>(It.IsAny<object>())).Returns(new Talk());
        _engagementService.Setup(s => s.SaveEngagementTalkAsync(It.IsAny<Talk>())).ReturnsAsync((Talk?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Add", redirectResult.ActionName);
    }
}
