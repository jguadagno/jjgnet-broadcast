using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Web.Controllers;
using JosephGuadagno.Broadcasting.Web.Interfaces;
using JosephGuadagno.Broadcasting.Web.Models;
using JosephGuadagno.Broadcasting.Web.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace JosephGuadagno.Broadcasting.Web.Tests.Controllers;

public class UserEventPublisherMappingControllerTests
{
    private readonly Mock<IUserEventPublisherMappingService> _mappingService = new();
    private readonly Mock<ISocialMediaPlatformService> _platformService = new();
    private readonly Mock<ISetupService> _setupService = new();
    private readonly UserEventPublisherMappingController _controller;

    public UserEventPublisherMappingControllerTests()
    {
        _controller = new UserEventPublisherMappingController(_mappingService.Object, _platformService.Object, _setupService.Object)
        {
            ControllerContext = WebControllerTestHelpers.CreateControllerContext("test-user-oid")
        };

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataFactory.GetTempData(httpContext);
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithDisplayMetadata()
    {
        _mappingService.Setup(service => service.GetAllAsync()).ReturnsAsync(
        [
            new UserEventPublisherMapping
            {
                Id = 1,
                EventType = "NewYouTubeItem",
                SocialMediaPlatformId = 5,
                IsActive = true,
                LastUpdatedOn = DateTimeOffset.UtcNow
            }
        ]);
        _platformService.Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items = [new SocialMediaPlatform { Id = 5, Name = "Bluesky", Icon = "bi-bluesky", IsActive = true }],
                TotalCount = 1
            });

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<UserEventPublisherMappingViewModel>>(viewResult.Model);
        model.Should().ContainSingle();
        model[0].EventTypeDisplayName.Should().Be("YouTube");
        model[0].SocialMediaPlatformName.Should().Be("Bluesky");
    }

    [Fact]
    public async Task Create_Post_WhenValid_ShouldRedirectToIndexAndInvalidateSetup()
    {
        UserEventPublisherMapping? captured = null;
        _mappingService.Setup(service => service.AddAsync(It.IsAny<UserEventPublisherMapping>()))
            .Callback<UserEventPublisherMapping>(mapping => captured = mapping)
            .ReturnsAsync(new UserEventPublisherMapping { Id = 9 });

        var viewModel = new UserEventPublisherMappingViewModel
        {
            EventType = "RandomPost",
            SocialMediaPlatformId = 8,
            IsActive = true
        };

        var result = await _controller.Create(viewModel);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(UserEventPublisherMappingController.Index));
        captured.Should().NotBeNull();
        captured!.EventType.Should().Be("RandomPost");
        captured.SocialMediaPlatformId.Should().Be(8);
        _setupService.Verify(service => service.InvalidateAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_Post_WhenModelStateInvalid_ShouldRepopulateOptions()
    {
        _controller.ModelState.AddModelError("EventType", "Required");
        _platformService.Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items = [new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }],
                TotalCount = 1
            });

        var result = await _controller.Create(new UserEventPublisherMappingViewModel());

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserEventPublisherMappingViewModel>(viewResult.Model);
        model.EventTypes.Should().NotBeEmpty();
        model.SocialMediaPlatforms.Should().NotBeEmpty();
        _mappingService.Verify(service => service.AddAsync(It.IsAny<UserEventPublisherMapping>()), Times.Never);
    }
}
