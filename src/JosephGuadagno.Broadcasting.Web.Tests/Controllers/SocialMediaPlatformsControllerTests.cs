using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class SocialMediaPlatformsControllerTests
{
    private readonly Mock<ISocialMediaPlatformService> _platformService;
    private readonly Mock<IMapper> _mapper;
    private readonly SocialMediaPlatformsController _controller;

    public SocialMediaPlatformsControllerTests()
    {
        _platformService = new Mock<ISocialMediaPlatformService>();
        _mapper = new Mock<IMapper>();
        _controller = new SocialMediaPlatformsController(_platformService.Object, _mapper.Object);

        // Initialise TempData (required by actions that set TempData["…"])
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

    // ── Index ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Index_ShouldReturnViewWithListOfViewModels()
    {
        // Arrange
        var platforms = new List<SocialMediaPlatform>
        {
            new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatform { Id = 2, Name = "BlueSky", IsActive = false }
        };
        var pagedResult = new PagedResult<SocialMediaPlatform> { Items = platforms, TotalCount = 2 };
        var viewModels = new List<SocialMediaPlatformViewModel>
        {
            new SocialMediaPlatformViewModel { Id = 1, Name = "Twitter", IsActive = true },
            new SocialMediaPlatformViewModel { Id = 2, Name = "BlueSky", IsActive = false }
        };
        _platformService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync(pagedResult);
        _mapper.Setup(m => m.Map<List<SocialMediaPlatformViewModel>>(platforms)).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().BeEquivalentTo(viewModels);
        _platformService.Verify(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Index_WhenNoPlatformsExist_ShouldReturnViewWithEmptyList()
    {
        // Arrange
        var emptyList = new List<SocialMediaPlatform>();
        var emptyPagedResult = new PagedResult<SocialMediaPlatform> { Items = emptyList, TotalCount = 0 };
        var emptyViewModels = new List<SocialMediaPlatformViewModel>();
        _platformService.Setup(s => s.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<bool>())).ReturnsAsync(emptyPagedResult);
        _mapper.Setup(m => m.Map<List<SocialMediaPlatformViewModel>>(emptyList)).Returns(emptyViewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().BeEquivalentTo(emptyViewModels);
    }

    // ── Add GET ───────────────────────────────────────────────────────────────

    [Fact]
    public void Add_Get_ShouldReturnViewWithIsActiveTrueByDefault()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<SocialMediaPlatformViewModel>(viewResult.Model);
        model.IsActive.Should().BeTrue("new platforms default to active");
    }

    [Fact]
    public void Add_Get_ShouldReturnViewWithNewViewModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().NotBeNull();
        viewResult.Model.Should().BeOfType<SocialMediaPlatformViewModel>();
    }

    // ── Add POST ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Add_Post_WhenModelStateIsValid_AndServiceSucceeds_ShouldRedirectToIndex()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel { Name = "Mastodon", IsActive = true };
        var domain = new SocialMediaPlatform { Name = "Mastodon", IsActive = true };
        var created = new SocialMediaPlatform { Id = 42, Name = "Mastodon", IsActive = true };
        _mapper.Setup(m => m.Map<SocialMediaPlatform>(viewModel)).Returns(domain);
        _platformService.Setup(s => s.AddAsync(domain)).ReturnsAsync(created);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _platformService.Verify(s => s.AddAsync(domain), Times.Once);
    }

    [Fact]
    public async Task Add_Post_WhenModelStateIsValid_AndServiceReturnsNull_ShouldReturnViewWithViewModel()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel { Name = "Mastodon", IsActive = true };
        var domain = new SocialMediaPlatform { Name = "Mastodon" };
        _mapper.Setup(m => m.Map<SocialMediaPlatform>(viewModel)).Returns(domain);
        _platformService.Setup(s => s.AddAsync(It.IsAny<SocialMediaPlatform>())).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Add_Post_WhenModelStateIsInvalid_ShouldReturnViewWithoutCallingService()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel(); // Name is required but empty
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _platformService.Verify(s => s.AddAsync(It.IsAny<SocialMediaPlatform>()), Times.Never);
    }

    // ── Edit GET ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_Get_WhenPlatformExists_ShouldReturnViewWithViewModel()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 1, Name = "LinkedIn", IsActive = true };
        var viewModel = new SocialMediaPlatformViewModel { Id = 1, Name = "LinkedIn", IsActive = true };
        _platformService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(platform);
        _mapper.Setup(m => m.Map<SocialMediaPlatformViewModel>(platform)).Returns(viewModel);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _platformService.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_WhenPlatformDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _platformService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _platformService.Verify(s => s.GetByIdAsync(99), Times.Once);
    }

    // ── Edit POST ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Edit_Post_WhenModelStateIsValid_AndServiceSucceeds_ShouldRedirectToIndex()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel { Id = 5, Name = "Twitter", IsActive = true };
        var domain = new SocialMediaPlatform { Id = 5, Name = "Twitter", IsActive = true };
        var updated = new SocialMediaPlatform { Id = 5, Name = "Twitter", IsActive = true };
        _mapper.Setup(m => m.Map<SocialMediaPlatform>(viewModel)).Returns(domain);
        _platformService.Setup(s => s.UpdateAsync(domain)).ReturnsAsync(updated);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _platformService.Verify(s => s.UpdateAsync(domain), Times.Once);
    }

    [Fact]
    public async Task Edit_Post_WhenModelStateIsValid_AndServiceReturnsNull_ShouldReturnViewWithViewModel()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel { Id = 5, Name = "Twitter" };
        var domain = new SocialMediaPlatform { Id = 5, Name = "Twitter" };
        _mapper.Setup(m => m.Map<SocialMediaPlatform>(viewModel)).Returns(domain);
        _platformService.Setup(s => s.UpdateAsync(It.IsAny<SocialMediaPlatform>())).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task Edit_Post_WhenModelStateIsInvalid_ShouldReturnViewWithoutCallingService()
    {
        // Arrange
        var viewModel = new SocialMediaPlatformViewModel { Id = 5 }; // Name is required
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _platformService.Verify(s => s.UpdateAsync(It.IsAny<SocialMediaPlatform>()), Times.Never);
    }

    // ── ToggleActive POST ─────────────────────────────────────────────────────

    [Fact]
    public async Task ToggleActive_WhenToggleSucceeds_ShouldRedirectToIndex()
    {
        // Arrange
        _platformService.Setup(s => s.ToggleActiveAsync(3)).ReturnsAsync(true);

        // Act
        var result = await _controller.ToggleActive(3);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
        _platformService.Verify(s => s.ToggleActiveAsync(3), Times.Once);
    }

    [Fact]
    public async Task ToggleActive_WhenToggleFails_ShouldRedirectToIndexWithError()
    {
        // Arrange
        _platformService.Setup(s => s.ToggleActiveAsync(99)).ReturnsAsync(false);

        // Act
        var result = await _controller.ToggleActive(99);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    // ── Delete GET ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_Get_WhenPlatformExists_ShouldReturnConfirmationView()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 7, Name = "Facebook", IsActive = true };
        var viewModel = new SocialMediaPlatformViewModel { Id = 7, Name = "Facebook", IsActive = true };
        _platformService.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(platform);
        _mapper.Setup(m => m.Map<SocialMediaPlatformViewModel>(platform)).Returns(viewModel);

        // Act
        var result = await _controller.Delete(7);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _platformService.Verify(s => s.GetByIdAsync(7), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_WhenPlatformDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _platformService.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _controller.Delete(999);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ── DeleteConfirmed POST ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteSucceeds_ShouldRedirectToIndexWithSuccess()
    {
        // Arrange
        _platformService.Setup(s => s.DeleteAsync(8)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(8);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _controller.TempData["SuccessMessage"].Should().NotBeNull();
        _platformService.Verify(s => s.DeleteAsync(8), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_AndPlatformStillExists_ShouldReturnViewWithError()
    {
        // Arrange
        var platform = new SocialMediaPlatform { Id = 8, Name = "Threads", IsActive = true };
        var viewModel = new SocialMediaPlatformViewModel { Id = 8, Name = "Threads", IsActive = true };
        _platformService.Setup(s => s.DeleteAsync(8)).ReturnsAsync(false);
        _platformService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync(platform);
        _mapper.Setup(m => m.Map<SocialMediaPlatformViewModel>(platform)).Returns(viewModel);

        // Act
        var result = await _controller.DeleteConfirmed(8);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        viewResult.Model.Should().Be(viewModel);
        _controller.ModelState.ErrorCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_AndPlatformNoLongerFound_ShouldRedirectToIndexWithError()
    {
        // Arrange
        _platformService.Setup(s => s.DeleteAsync(8)).ReturnsAsync(false);
        _platformService.Setup(s => s.GetByIdAsync(8)).ReturnsAsync((SocialMediaPlatform?)null);

        // Act
        var result = await _controller.DeleteConfirmed(8);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(SocialMediaPlatformsController.Index));
        _controller.TempData["ErrorMessage"].Should().NotBeNull();
    }

    // ── Authorization attribute assertions ────────────────────────────────────

    [Fact]
    public void SocialMediaPlatformsController_ClassLevel_ShouldRequireViewerPolicy()
    {
        // Arrange & Act
        var attributes = typeof(SocialMediaPlatformsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);

        // Assert
        attributes.Should().NotBeEmpty();
        var authorizeAttribute = (AuthorizeAttribute)attributes.First();
        authorizeAttribute.Policy.Should().Be(AuthorizationPolicyNames.RequireViewer);
    }

    [Fact]
    public void Add_Get_Action_ShouldRequireContributorPolicy()
    {
        // Arrange & Act
        var method = typeof(SocialMediaPlatformsController).GetMethod("Add", Type.EmptyTypes);

        // Assert
        method.Should().NotBeNull();
        var attributes = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);
        attributes.Should().NotBeEmpty();
        var authorizeAttribute = (AuthorizeAttribute)attributes.First();
        authorizeAttribute.Policy.Should().Be(AuthorizationPolicyNames.RequireContributor);
    }

    [Fact]
    public void Delete_Get_Action_ShouldRequireSiteAdministratorPolicy()
    {
        // Arrange & Act
        var method = typeof(SocialMediaPlatformsController).GetMethod("Delete", new[] { typeof(int) });

        // Assert
        method.Should().NotBeNull();
        var attributes = method!.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false);
        attributes.Should().NotBeEmpty();
        var authorizeAttribute = (AuthorizeAttribute)attributes.First();
        authorizeAttribute.Policy.Should().Be(AuthorizationPolicyNames.RequireSiteAdministrator);
    }
}
