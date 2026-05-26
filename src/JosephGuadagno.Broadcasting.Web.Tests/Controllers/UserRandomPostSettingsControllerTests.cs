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

public class UserRandomPostSettingsControllerTests
{
    private readonly Mock<IUserRandomPostSettingsService> _settingsService = new();
    private readonly Mock<ISocialMediaPlatformService> _platformService = new();
    private readonly Mock<ISetupService> _setupService = new();
    private readonly UserRandomPostSettingsController _controller;

    public UserRandomPostSettingsControllerTests()
    {
        _controller = new UserRandomPostSettingsController(_settingsService.Object, _platformService.Object, _setupService.Object)
        {
            ControllerContext = WebControllerTestHelpers.CreateControllerContext("test-user-oid")
        };

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempDataFactory = new TempDataDictionaryFactory(tempDataProvider.Object);
        _controller.TempData = tempDataFactory.GetTempData(httpContext);
    }

    [Fact]
    public async Task Index_ShouldReturnViewWithPlatformNames()
    {
        _settingsService.Setup(service => service.GetAllAsync()).ReturnsAsync(
        [
            new UserRandomPostSettings
            {
                Id = 1,
                SocialMediaPlatformId = 7,
                CronExpression = "0 * * * *",
                ExcludedCategories = ["Announcements"],
                IsActive = true,
                LastUpdatedOn = DateTimeOffset.UtcNow
            }
        ]);
        _platformService.Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items = [new SocialMediaPlatform { Id = 7, Name = "Twitter", Icon = "bi-twitter-x", IsActive = true }],
                TotalCount = 1
            });

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<UserRandomPostSettingsViewModel>>(viewResult.Model);
        model.Should().ContainSingle();
        model[0].SocialMediaPlatformName.Should().Be("Twitter");
    }

    [Fact]
    public async Task Create_Post_ShouldConvertCutoffDateToUtcAndRedirect()
    {
        UserRandomPostSettings? captured = null;
        _settingsService.Setup(service => service.AddAsync(It.IsAny<UserRandomPostSettings>()))
            .Callback<UserRandomPostSettings>(settings => captured = settings)
            .ReturnsAsync(new UserRandomPostSettings { Id = 5 });

        var viewModel = new UserRandomPostSettingsViewModel
        {
            SocialMediaPlatformId = 3,
            CronExpression = "15 * * * *",
            CutoffDateUtc = "2026-05-26T18:30:00.0000000Z",
            ExcludedCategoriesText = "Books, Archive",
            IsActive = true
        };

        var result = await _controller.Create(viewModel);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        redirectResult.ActionName.Should().Be(nameof(UserRandomPostSettingsController.Index));
        captured.Should().NotBeNull();
        captured!.CutoffDate.Should().Be(DateTimeOffset.Parse("2026-05-26T18:30:00.0000000Z"));
        captured.ExcludedCategories.Should().BeEquivalentTo(["Books", "Archive"]);
        _setupService.Verify(service => service.InvalidateAsync(), Times.Once);
    }

    [Fact]
    public async Task Create_Post_WhenModelStateInvalid_ShouldRepopulatePlatforms()
    {
        _controller.ModelState.AddModelError("CronExpression", "Required");
        _platformService.Setup(service => service.GetAllAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedResult<SocialMediaPlatform>
            {
                Items = [new SocialMediaPlatform { Id = 1, Name = "Twitter", IsActive = true }],
                TotalCount = 1
            });

        var result = await _controller.Create(new UserRandomPostSettingsViewModel());

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<UserRandomPostSettingsViewModel>(viewResult.Model);
        model.SocialMediaPlatforms.Should().NotBeEmpty();
        _settingsService.Verify(service => service.AddAsync(It.IsAny<UserRandomPostSettings>()), Times.Never);
    }
}
