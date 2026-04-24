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
using JosephGuadagno.Broadcasting.Web.Tests.Helpers;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class SchedulesControllerTests
{
    private readonly Mock<IScheduledItemService> _scheduledItemService;
    private readonly Mock<IScheduledItemValidationService> _validationService;
    private readonly Mock<IEngagementService> _engagementService;
    private readonly Mock<ISyndicationFeedSourceService> _syndicationFeedSourceService;
    private readonly Mock<IYouTubeSourceService> _youTubeSourceService;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<ILogger<SchedulesController>> _logger;
    private readonly SchedulesController _controller;

    public SchedulesControllerTests()
    {
        _scheduledItemService = new Mock<IScheduledItemService>();
        _validationService = new Mock<IScheduledItemValidationService>();
        _engagementService = new Mock<IEngagementService>();
        _syndicationFeedSourceService = new Mock<ISyndicationFeedSourceService>();
        _youTubeSourceService = new Mock<IYouTubeSourceService>();
        _mapper = new Mock<IMapper>();
        _logger = new Mock<ILogger<SchedulesController>>();
        _controller = new SchedulesController(
            _scheduledItemService.Object,
            _validationService.Object,
            _mapper.Object,
            _logger.Object,
            _engagementService.Object,
            _syndicationFeedSourceService.Object,
            _youTubeSourceService.Object);
        
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
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "test-oid" };
        var viewModel = new ScheduledItemViewModel { Id = 1 };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, "test-oid") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

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
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "test-oid" };
        var viewModel = new ScheduledItemViewModel { Id = 1 };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, "test-oid") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

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
        var userOid = "test-user-oid";
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        var existingItem = new ScheduledItem { Id = 1, CreatedByEntraOid = userOid };
        var savedItem = new ScheduledItem { Id = 1 };

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

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(existingItem);
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
        var userOid = "test-user-oid";
        var viewModel = new ScheduledItemViewModel { Id = 7 };
        var existingItem = new ScheduledItem { Id = 7, CreatedByEntraOid = userOid };

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

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(7)).ReturnsAsync(existingItem);
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
    public async Task Edit_Post_WhenScheduledItemNotFound_ShouldReturnNotFound()
    {
        // Arrange — issue #742: defence-in-depth fetch returns NotFound
        var viewModel = new ScheduledItemViewModel { Id = 99 };
        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(99)).ReturnsAsync((ScheduledItem?)null);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        Assert.IsType<NotFoundResult>(result);
        _scheduledItemService.Verify(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsNotOwnerAndNotAdmin_ShouldRedirectWithError()
    {
        // Arrange — issue #742: ownership re-verification prevents save by non-owner
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        var existingItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "owner-oid-12345" };

        // User OID "non-owner-oid-99999" does not match entity's "owner-oid-12345".
        _controller.ControllerContext = WebControllerTestHelpers.CreateNonOwnerControllerContext();

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(existingItem);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to edit this scheduled item.", _controller.TempData["ErrorMessage"]);
        _scheduledItemService.Verify(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Post_WhenUserIsSiteAdministrator_ShouldAllowSaveRegardlessOfOwnership()
    {
        // Arrange — issue #742: SiteAdministrators bypass ownership check
        var viewModel = new ScheduledItemViewModel { Id = 1 };
        var existingItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "another-user-oid" };
        var savedItem = new ScheduledItem { Id = 1 };

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

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(existingItem);
        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService.Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(savedItem);

        // Act
        var result = await _controller.Edit(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        _scheduledItemService.Verify(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Get_ShouldReturnConfirmationView()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "test-oid" };
        var viewModel = new ScheduledItemViewModel { Id = 1 };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, "test-oid") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

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

    [Fact]
    public async Task Details_WhenUserIsNotOwnerAndNotAdmin_RedirectsWithError()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "different-user-oid" };
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid-12345"),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);

        // Act
        var result = await _controller.Details(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to view this scheduled item.", _controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Edit_Get_WhenUserIsNotOwnerAndNotAdmin_RedirectsWithError()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "different-user-oid" };
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid-12345"),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);

        // Act
        var result = await _controller.Edit(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to edit this scheduled item.", _controller.TempData["ErrorMessage"]);
        _mapper.Verify(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Delete_Get_WhenUserIsNotOwnerAndNotAdmin_RedirectsWithError()
    {
        // Arrange
        var scheduledItem = new ScheduledItem { Id = 1, CreatedByEntraOid = "different-user-oid" };
        var claims = new List<Claim>
        {
            new Claim(ApplicationClaimTypes.EntraObjectId, "user-oid-12345"),
            new Claim(ClaimTypes.Role, RoleNames.Contributor)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        _scheduledItemService.Setup(s => s.GetScheduledItemAsync(1)).ReturnsAsync(scheduledItem);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        Assert.Equal("You do not have permission to delete this scheduled item.", _controller.TempData["ErrorMessage"]);
        _mapper.Verify(m => m.Map<ScheduledItemViewModel>(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Add_Post_SetsCreatedByEntraOid()
    {
        // Arrange
        var userOid = "user-oid-67890";
        var viewModel = new ScheduledItemViewModel { Id = 0 };
        var savedItem = new ScheduledItem { Id = 55, CreatedByEntraOid = userOid };

        var claims = new List<Claim> { new Claim(ApplicationClaimTypes.EntraObjectId, userOid) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };

        ScheduledItem? capturedItem = null;
        _mapper.Setup(m => m.Map<ScheduledItem>(It.IsAny<object>())).Returns(new ScheduledItem());
        _scheduledItemService
            .Setup(s => s.SaveScheduledItemAsync(It.IsAny<ScheduledItem>()))
            .Callback<ScheduledItem>(item => capturedItem = item)
            .ReturnsAsync(savedItem);

        // Act
        var result = await _controller.Add(viewModel);

        // Assert
        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Details", redirectResult.ActionName);
        Assert.NotNull(capturedItem);
        Assert.Equal(userOid, capturedItem!.CreatedByEntraOid);
    }

    // -------------------------------------------------------------------------
    // Source search actions (#810)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SearchEngagements_WithQuery_ReturnsMatchingEngagements()
    {
        // Arrange
        var engagements = new List<Engagement>
        {
            new Engagement { Id = 1, Name = "TechConf 2024" },
            new Engagement { Id = 2, Name = "DevFest 2024" }
        };
        var pagedResult = new PagedResult<Engagement> { Items = engagements, TotalCount = 2 };
        _engagementService.Setup(s => s.GetEngagementsAsync(1, 20, "name", false, "Tech")).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.SearchEngagements("Tech");

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task GetTalksByEngagement_WithValidId_ReturnsTalks()
    {
        // Arrange
        var talks = new List<Talk>
        {
            new Talk { Id = 10, Name = "Intro to .NET" },
            new Talk { Id = 11, Name = "Advanced Blazor" }
        };
        var pagedResult = new PagedResult<Talk> { Items = talks, TotalCount = 2 };
        _engagementService.Setup(s => s.GetEngagementTalksAsync(5, 1, 50)).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetTalksByEngagement(5);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task GetTalksByEngagement_WithZeroId_ReturnsEmptyArray()
    {
        // Act
        var result = await _controller.GetTalksByEngagement(0);

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        _engagementService.Verify(s => s.GetEngagementTalksAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task SearchSyndicationFeedSources_WithQuery_ReturnsFilteredSources()
    {
        // Arrange
        var sources = new List<SyndicationFeedSource>
        {
            new SyndicationFeedSource { Id = 1, Title = "Tech Blog", FeedIdentifier = "f1", Author = "A", Url = "http://a.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow, CreatedByEntraOid = "oid" },
            new SyndicationFeedSource { Id = 2, Title = "News Feed", FeedIdentifier = "f2", Author = "B", Url = "http://b.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow, CreatedByEntraOid = "oid" }
        };
        _syndicationFeedSourceService.Setup(s => s.GetAllAsync()).ReturnsAsync(sources);

        // Act
        var result = await _controller.SearchSyndicationFeedSources("Tech");

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task SearchYouTubeSources_WithQuery_ReturnsFilteredSources()
    {
        // Arrange
        var sources = new List<YouTubeSource>
        {
            new YouTubeSource { Id = 1, Title = "Intro Video", VideoId = "v1", Author = "A", Url = "http://a.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow, CreatedByEntraOid = "oid" },
            new YouTubeSource { Id = 2, Title = "Tutorial", VideoId = "v2", Author = "B", Url = "http://b.com", PublicationDate = DateTimeOffset.UtcNow, AddedOn = DateTimeOffset.UtcNow, LastUpdatedOn = DateTimeOffset.UtcNow, CreatedByEntraOid = "oid" }
        };
        _youTubeSourceService.Setup(s => s.GetAllAsync()).ReturnsAsync(sources);

        // Act
        var result = await _controller.SearchYouTubeSources("Intro");

        // Assert
        var jsonResult = Assert.IsType<JsonResult>(result);
        Assert.NotNull(jsonResult.Value);
    }
}
