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

public class SchedulesControllerTests
{
    private readonly Mock<IScheduledItemService> _scheduledItemService;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<SchedulesController>> _logger;
    private readonly SchedulesController _controller;

    public SchedulesControllerTests()
    {
        _scheduledItemService = new Mock<IScheduledItemService>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<SchedulesController>>();
        _controller = new SchedulesController(_scheduledItemService.Object, _mapper.Object, _logger.Object);
        
        // Initialize TempData
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataDictionaryFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataDictionaryFactory.GetTempData(httpContext);
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithScheduledItemViewModels()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = scheduledItems, TotalCount = scheduledItems.Count };
        var orphanedResult = new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 };
        var viewModels = new List<ScheduledItemViewModel> { new ScheduledItemViewModel { Id = 1 } };
        _scheduledItemService.Setup(s => s.GetScheduledItemsAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(pagedScheduledItems);
        _scheduledItemService.Setup(s => s.GetOrphanedScheduledItemsAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(orphanedResult);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _scheduledItemService.Verify(s => s.GetScheduledItemsAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Details_WhenScheduledItemFound_ShouldReturnViewWithScheduledItemViewModel()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);
        _mapper.Setup(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _scheduledItemService.Verify(s => s.GetScheduledItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task Details_WhenScheduledItemNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(99)).ReturnsAsync((ScheduledItem?)null);

        // Act
        var result = await _controller.Details(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Get_WhenScheduledItemFound_ShouldReturnViewWithScheduledItemViewModel()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);
        _mapper.Setup(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModel, viewResult.Model);
        _scheduledItemService.Verify(s => s.GetScheduledItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task Edit_Get_WhenScheduledItemNotFound_ShouldReturnNotFound()
    {
        // Arrange
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(99)).ReturnsAsync((ScheduledItem?)null);

        // Act
        var result = await _controller.Edit(99);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Edit_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        var savedItem = new ScheduledItem { Id = 1 };
        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService.Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(savedItem);

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
        var viewModel = new ScheduledItemViewModel { Id = 7 };
        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService.Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>())).ReturnsAsync((ScheduledItem?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirectResult.ActionName);
        Assert.Equal(7, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnConfirmationView()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1 };
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);
        _mapper.Setup(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>())).Returns(viewModel);

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
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "user-oid" };

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

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);
        _scheduledItemService.Setup(s => s.DeleteScheduledItemAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _scheduledItemService.Verify(s => s.DeleteScheduledItemAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteConfirmed_WhenDeleteFails_ShouldReturnView()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "user-oid" };
        var viewModel = new ScheduledItemViewModel { Id = 1 };

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

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);
        _scheduledItemService.Setup(s => s.DeleteScheduledItemAsync(1)).ReturnsAsync(false);
        _mapper.Setup(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>())).Returns(viewModel);

        // Act
        var result = await _controller.DeleteConfirmed(1);

        // Assert
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Add_Get_ShouldReturnViewWithNewScheduledItemViewModel()
    {
        // Act
        var result = _controller.Add();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ScheduledItemViewModel>(viewResult.Model);
        Assert.NotNull(model);
    }

    [Fact]
    public async Task Add_Post_WhenSaveSucceeds_ShouldRedirectToDetails()
    {
        // Arrange
        var viewModel = new ScheduledItemViewModel { Id = 0 };
        var savedItem = new ScheduledItem { Id = 55 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService.Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(savedItem);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.Equal(55, redirectResult.RouteValues?["id"]);
    }

    [Fact]
    public async Task Add_Post_WhenSaveFails_ShouldRedirectToAdd()
    {
        // Arrange
        var viewModel = new ScheduledItemViewModel { Id = 0 };

        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService.Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>())).ReturnsAsync((ScheduledItem?)null);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Add", redirectResult.ActionName);
    }

    [Fact]
    public async Task Calendar_WhenNoParamsProvided_ShouldUseCurrentYearAndMonth()
    {
        // Arrange
        var expectedYear = DateTime.Today.Year;
        var expectedMonth = DateTime.Today.Month;
        var viewModels = new List<ScheduledItemViewModel>();
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 };
        _scheduledItemService
            .Setup(s => s.GetScheduledItemsByCalendarMonthAsync(expectedYear, expectedMonth, It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Calendar(null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedYear, viewResult.ViewData["Year"]);
        Assert.Equal(expectedMonth, viewResult.ViewData["Month"]);
        _scheduledItemService.Verify(s => s.GetScheduledItemsByCalendarMonthAsync(expectedYear, expectedMonth, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Calendar_WhenValidParamsProvided_ShouldUseProvidedYearAndMonth()
    {
        // Arrange
        var viewModels = new List<ScheduledItemViewModel>();
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 };
        _scheduledItemService
            .Setup(s => s.GetScheduledItemsByCalendarMonthAsync(2024, 6, It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Calendar(2024, 6);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(2024, viewResult.ViewData["Year"]);
        Assert.Equal(6, viewResult.ViewData["Month"]);
        _scheduledItemService.Verify(s => s.GetScheduledItemsByCalendarMonthAsync(2024, 6, It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Calendar_WhenYearIsZeroOrNegative_ShouldUseMinValueYear()
    {
        // Arrange
        var expectedYear = DateTime.MinValue.Year;
        var expectedMonth = 6;
        var viewModels = new List<ScheduledItemViewModel>();
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 };
        _scheduledItemService
            .Setup(s => s.GetScheduledItemsByCalendarMonthAsync(expectedYear, expectedMonth, It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Calendar(0, 6);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedYear, viewResult.ViewData["Year"]);
    }

    [Fact]
    public async Task Calendar_WhenMonthIsOutOfRange_ShouldUseTodayMonth()
    {
        // Arrange
        var expectedMonth = DateTime.Today.Month;
        var viewModels = new List<ScheduledItemViewModel>();
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 };
        _scheduledItemService
            .Setup(s => s.GetScheduledItemsByCalendarMonthAsync(It.IsAny<int>(), expectedMonth, It.IsAny<int?>(), It.IsAny<int?>()))
            .ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Calendar(2024, 13);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(expectedMonth, viewResult.ViewData["Month"]);
    }

    [Fact]
    public async Task Unsent_ShouldReturnViewWithUnsentScheduledItemViewModels()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 1 } };
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = scheduledItems, TotalCount = scheduledItems.Count };
        var viewModels = new List<ScheduledItemViewModel> { new ScheduledItemViewModel { Id = 1 } };
        _scheduledItemService.Setup(s => s.GetUnsentScheduledItemsAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Unsent();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _scheduledItemService.Verify(s => s.GetUnsentScheduledItemsAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }

    [Fact]
    public async Task Upcoming_ShouldReturnViewWithUpcomingScheduledItemViewModels()
    {
        // Arrange
        var scheduledItems = new List<ScheduledItem> { new ScheduledItem { Id = 2 } };
        var pagedScheduledItems = new PagedResult<ScheduledItem> { Items = scheduledItems, TotalCount = scheduledItems.Count };
        var viewModels = new List<ScheduledItemViewModel> { new ScheduledItemViewModel { Id = 2 } };
        _scheduledItemService.Setup(s => s.GetScheduledItemsToSendAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(pagedScheduledItems);
        _mapper.Setup(m => m.Map<List<ScheduledItemViewModel>>(It.IsAny<object>())).Returns(viewModels);

        // Act
        var result = await _controller.Upcoming();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(viewModels, viewResult.Model);
        _scheduledItemService.Verify(s => s.GetScheduledItemsToSendAsync(It.IsAny<int?>(), It.IsAny<int?>()), Times.Once);
    }
}
