using Moq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using JosephGuadagno.Broadcasting.Domain.Models;
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
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithEngagementViewModels()
    {
        // Arrange
        var engagements = new List<Engagement> { new Engagement { Id = 1 } };
        var viewModels = new List<EngagementViewModel> { new EngagementViewModel { Id = 1 } };
        _engagementService.Setup(s => s.GetEngagementsAsync()).ReturnsAsync(engagements);
        _mapper.Setup(m => m.Map<List<EngagementViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _engagementService.Verify(s => s.GetEngagementsAsync(), Times.Once);
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
        var engagement = new Engagement { Id = 1 };
        var viewModel = new EngagementViewModel { Id = 1 };
        _engagementService.Setup(s => s.DeleteEngagementAsync(1)).ReturnsAsync(false);
        _engagementService.Setup(s => s.GetEngagementAsync(1)).ReturnsAsync(engagement);
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
        _mapper.Setup(m => m.Map<Engagement>(It.IsAny<object>())).Returns(new Engagement());
        _engagementService.Setup(s => s.SaveEngagementAsync(It.IsAny<Engagement>())).ReturnsAsync((Engagement?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Add", redirectResult.ActionName);
    }
}
