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
using JosephGuadagno.Broadcasting.Web.Tests.Helpers;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

/// <summary>
/// Unit tests for <see cref="SyndicationFeedSourcesController"/>.
///
/// Security coverage matrix (ownership enforcement — redirect pattern):
/// | Call site                          | Test name                                                                        |
/// |------------------------------------|----------------------------------------------------------------------------------|
/// | Details — non-owner redirect       | Details_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError                    |
/// | Details — admin bypass             | Details_WhenUserIsSiteAdministrator_ShouldReturnView                             |
/// | Delete GET — non-owner redirect    | Delete_Get_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError                 |
/// | Delete GET — admin bypass          | Delete_Get_WhenUserIsSiteAdministrator_ShouldReturnView                          |
/// | DeleteConfirmed — non-owner        | DeleteConfirmed_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError             |
/// | DeleteConfirmed — admin bypass     | DeleteConfirmed_WhenUserIsSiteAdministrator_DeletesAnySource                     |
/// </summary>
public class SyndicationFeedSourcesControllerTests
{
    private readonly Mock<ISyndicationFeedSourceService> _service;
    private readonly Mock<IMapper> _mapper;
    private readonly SyndicationFeedSourcesController _controller;

    public SyndicationFeedSourcesControllerTests()
    {
        _service = new Mock<ISyndicationFeedSourceService>();
        _mapper = new Mock<IMapper>();
        _controller = new SyndicationFeedSourcesController(_service.Object, _mapper.Object);

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static SyndicationFeedSource BuildSource(int id, string oid = "owner-oid-12345") =>
        new SyndicationFeedSource
        {
            Id = id,
            CreatedByEntraOid = oid,
            FeedIdentifier = "feed-id-001",
            Author = "Test Author",
            Title = "Test Title",
            Url = "https://example.com/feed",
            PublicationDate = DateTimeOffset.UtcNow,
            AddedOn = DateTimeOffset.UtcNow,
            LastUpdatedOn = DateTimeOffset.UtcNow
        };

    private void SetOwnerUser(string oid = "owner-oid-12345", string role = RoleNames.Contributor)
    {
        _controller.ControllerContext = WebControllerTestHelpers.CreateControllerContext(oid, role);
    }

    private void SetAdminUser()
    {
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
    }

    // -------------------------------------------------------------------------
    // Index
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Index_ShouldReturnViewWithSyndicationFeedSourceViewModels()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource> { BuildSource(1) };
        var viewModels = new List<SyndicationFeedSourceViewModel> { new SyndicationFeedSourceViewModel { Id = 1 } };
        _service.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()))
            .ReturnsAsync(new PagedResult<SyndicationFeedSource> { Items = sources, TotalCount = sources.Count });
        _mapper.Setup(m => m.Map<List<SyndicationFeedSourceViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _service.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Details
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Details_WhenSourceFound_AndUserIsOwner_ShouldReturnView()
    {
        // Arrange
        var oid = "owner-oid-12345";
        var source = BuildSource(1, oid);
        var viewModel = new SyndicationFeedSourceViewModel { Id = 1 };
        SetOwnerUser(oid);
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _mapper.Setup(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task Details_WhenSourceNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.Setup(s => s.GetAsync(99)).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _controller.Details(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Details_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — caller OID "non-owner-oid-99999" does not match entity's "owner-oid-12345"
        var source = BuildSource(1, oid: "owner-oid-12345");
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("You do not have permission to view this syndication feed source.", _controller.TempData["ErrorMessage"]);
        _mapper.Verify(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Details_WhenUserIsSiteAdministrator_ShouldReturnView()
    {
        // Arrange — admin sees any source regardless of ownership
        var source = BuildSource(1, oid: "another-user-oid");
        var viewModel = new SyndicationFeedSourceViewModel { Id = 1 };
        SetAdminUser();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _mapper.Setup(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    // -------------------------------------------------------------------------
    // Add (GET)
    // -------------------------------------------------------------------------

    [Fact]
    public void Add_Get_ShouldReturnViewWithNewSyndicationFeedSourceViewModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<SyndicationFeedSourceViewModel>(viewResult.Model);
    }

    // -------------------------------------------------------------------------
    // Add (POST)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Add_Post_WhenModelStateIsInvalid_ShouldReturnView()
    {
        // Arrange
        _controller.ModelState.AddModelError("Title", "Required");
        var viewModel = new SyndicationFeedSourceViewModel();

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _service.Verify(s => s.SaveAsync(It.IsAny<SyndicationFeedSource>()), Times.Never);
    }

    [Fact]
    public async Task Add_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var viewModel = new SyndicationFeedSourceViewModel { Id = 0 };
        var savedSource = BuildSource(42);
        SetOwnerUser();
        _mapper.Setup(m => m.Map<SyndicationFeedSource>(It.IsAny<object>())).Returns(BuildSource(0));
        _service.Setup(s => s.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync(savedSource);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirect.ActionName);
        Assert.Equal(42, redirect.RouteValues?["id"]);
        Assert.Equal("Syndication feed source added successfully.", _controller.TempData["SuccessMessage"]);
    }

    [Fact]
    public async Task Add_Post_WhenSaveFails_ShouldRedirectToAdd()
    {
        // Arrange
        var viewModel = new SyndicationFeedSourceViewModel { Id = 0 };
        SetOwnerUser();
        _mapper.Setup(m => m.Map<SyndicationFeedSource>(It.IsAny<object>())).Returns(BuildSource(0));
        _service.Setup(s => s.SaveAsync(It.IsAny<SyndicationFeedSource>())).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Add", redirect.ActionName);
        Assert.Equal("Failed to add the syndication feed source.", _controller.TempData["ErrorMessage"]);
    }

    // -------------------------------------------------------------------------
    // Delete (GET)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Delete_Get_WhenSourceFound_AndUserIsOwner_ShouldReturnView()
    {
        // Arrange
        var oid = "owner-oid-12345";
        var source = BuildSource(1, oid);
        var viewModel = new SyndicationFeedSourceViewModel { Id = 1 };
        SetOwnerUser(oid);
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _mapper.Setup(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    [Fact]
    public async Task Delete_Get_WhenSourceNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.Setup(s => s.GetAsync(99)).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _controller.Delete(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Delete_Get_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — caller OID "non-owner-oid-99999" does not match entity's "owner-oid-12345"
        var source = BuildSource(1, oid: "owner-oid-12345");
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("You do not have permission to delete this syndication feed source.", _controller.TempData["ErrorMessage"]);
        _mapper.Verify(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Get_WhenUserIsSiteAdministrator_ShouldReturnView()
    {
        // Arrange — admin can delete any source regardless of ownership
        var source = BuildSource(1, oid: "another-user-oid");
        var viewModel = new SyndicationFeedSourceViewModel { Id = 1 };
        SetAdminUser();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _mapper.Setup(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
    }

    // -------------------------------------------------------------------------
    // DeleteConfirmed (POST)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteConfirmed_WhenSourceNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _service.Setup(s => s.GetAsync(99)).ReturnsAsync((SyndicationFeedSource?)null);

        // Act
        var result = await _controller.DeleteConfirmed(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _service.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — caller OID "non-owner-oid-99999" does not match entity's "owner-oid-12345"
        var source = BuildSource(1, oid: "owner-oid-12345");
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("You do not have permission to delete this syndication feed source.", _controller.TempData["ErrorMessage"]);
        _service.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteSucceeds_ShouldRedirectToIndex()
    {
        // Arrange
        var oid = "owner-oid-12345";
        var source = BuildSource(1, oid);
        SetOwnerUser(oid);
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _service.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Syndication feed source deleted successfully.", _controller.TempData["SuccessMessage"]);
        _service.Verify(s => s.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_ShouldReturnView()
    {
        // Arrange
        var oid = "owner-oid-12345";
        var source = BuildSource(1, oid);
        var viewModel = new SyndicationFeedSourceViewModel { Id = 1 };
        SetOwnerUser(oid);
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _service.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<SyndicationFeedSourceViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenUserIsSiteAdministrator_DeletesAnySource()
    {
        // Arrange — admin deletes a source owned by someone else
        var source = BuildSource(1, oid: "another-user-oid");
        SetAdminUser();
        _service.Setup(s => s.GetAsync(1)).ReturnsAsync(source);
        _service.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        _service.Verify(s => s.DeleteAsync(1), Times.Once);
    }
}
