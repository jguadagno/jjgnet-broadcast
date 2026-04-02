using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class EngagementsControllerTests
{
    private readonly Mock<IEngagementService> _engagementService;
    private readonly Mock<IMapper> _mapper;
    private readonly EngagementsController _controller;

    public EngagementsControllerTests()
    {
        _engagementService = new Mock<IEngagementService>();
        _mapper = new Mock<IMapper>();
        _controller = new EngagementsController(_engagementService.Object, _mapper.Object);
        
        // Initialize TempData
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

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
        var engagement = new Engagement { Id = 1 };
        var viewModel = new EngagementViewModel { Id = 1 };
        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);

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
        var engagement = new Engagement { Id = 1 };
        var viewModel = new EngagementViewModel { Id = 1 };
        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
        _mapper.Setup(m => m.Map<EngagementViewModel>(It.IsAny<object>())).Returns(viewModel);

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
        var viewModel = new EngagementViewModel { Id = 1 };
        var savedEngagement = new Engagement { Id = 1 };
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
        var viewModel = new EngagementViewModel { Id = 5 };
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
    public async Task Delete_Get_ShouldReturnConfirmationView()
    {
        // Arrange
        var engagement = new Engagement { Id = 1 };
        var viewModel = new EngagementViewModel { Id = 1 };
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
    public async Task DeleteConfirmed_WhenUserIsAdministrator_DeletesAnyEngagement()
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
            new Claim(ClaimTypes.Role, RoleNames.Administrator)
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
    public async Task DeleteConfirmed_WhenUserIsNotOwnerAndNotAdmin_ReturnsForbid()
    {
        // Arrange
        var engagementId = 1;
        var userOid = "user-oid-12345";
        var engagement = new Engagement
        {
            Id = engagementId,
            CreatedByEntraOid = "different-user-oid"
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

        // Act
        var result = await _controller.DeleteConfirmed(engagementId);

        // Assert
        Assert.IsType<ForbidResult>(result);
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
}
