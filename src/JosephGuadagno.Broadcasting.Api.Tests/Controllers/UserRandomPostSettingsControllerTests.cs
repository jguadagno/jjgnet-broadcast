using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers.Publishers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class UserRandomPostSettingsControllerTests
{
    private readonly Mock<IUserRandomPostSettingsManager> _managerMock = new();
    private readonly Mock<IOnboardingManager> _onboardingManagerMock = new();
    private readonly Mock<ILogger<UserRandomPostSettingsController>> _loggerMock = new();

    private static readonly IMapper Mapper = ApiTestMapper.Instance;

    private UserRandomPostSettingsController CreateSut(string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        return new UserRandomPostSettingsController(
            _managerMock.Object,
            _onboardingManagerMock.Object,
            _loggerMock.Object,
            Mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
    }

    private static UserRandomPostSettings BuildSettings(int id = 1, string ownerOid = "owner-oid-12345") => new()
    {
        Id = id,
        CreatedByEntraOid = ownerOid,
        SocialMediaPlatformId = 2,
        CronExpression = "0 * * * *",
        CutoffDate = new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
        ExcludedCategories = ["Announcements", "Events"],
        IsActive = true,
        CreatedOn = new DateTimeOffset(2026, 05, 01, 0, 0, 0, TimeSpan.Zero),
        LastUpdatedOn = new DateTimeOffset(2026, 05, 02, 0, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public async Task GetAllAsync_WhenItemsExist_ReturnsOkWithItems()
    {
        var items = new List<UserRandomPostSettings>
        {
            BuildSettings(1),
            BuildSettings(2)
        };
        _managerMock.Setup(m => m.GetByUserAsync("owner-oid-12345", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var sut = CreateSut();

        var result = await sut.GetAllAsync();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<UserRandomPostSettingsResponse>>().Subject;
        response.Should().HaveCount(2);
        response.Should().BeEquivalentTo(items, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetAllAsync_WhenNoItemsExist_ReturnsOkWithEmptyList()
    {
        _managerMock.Setup(m => m.GetByUserAsync("owner-oid-12345", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var sut = CreateSut();

        var result = await sut.GetAllAsync();

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<List<UserRandomPostSettingsResponse>>().Subject;
        response.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WhenItemExistsAndOwnedByCaller_ReturnsOk()
    {
        var settings = BuildSettings(7);
        _managerMock.Setup(m => m.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        var sut = CreateSut();

        var result = await sut.GetAsync(7);

        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(settings, options => options.ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        _managerMock.Setup(m => m.GetByIdAsync(8, It.IsAny<CancellationToken>())).ReturnsAsync((UserRandomPostSettings?)null);

        var sut = CreateSut();

        var result = await sut.GetAsync(8);

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAsync_WhenItemBelongsToAnotherUser_ReturnsForbid()
    {
        _managerMock.Setup(m => m.GetByIdAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync(BuildSettings(9, ownerOid: "other-owner"));

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        var result = await sut.GetAsync(9);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task CreateAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        var sut = CreateSut();
        sut.ModelState.AddModelError("CronExpression", "CronExpression is required");

        var result = await sut.CreateAsync(new CreateUserRandomPostSettingsRequest());

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _managerMock.Verify(m => m.SaveAsync(It.IsAny<UserRandomPostSettings>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenSaveSucceeds_ReturnsCreatedAtAction()
    {
        var request = new CreateUserRandomPostSettingsRequest
        {
            SocialMediaPlatformId = 4,
            CronExpression = "15 * * * *",
            ExcludedCategories = ["Podcasts"],
            IsActive = true
        };
        var saved = BuildSettings(10);
        saved.SocialMediaPlatformId = 4;
        saved.CronExpression = "15 * * * *";
        saved.ExcludedCategories = ["Podcasts"];

        UserRandomPostSettings? captured = null;
        _managerMock.Setup(m => m.SaveAsync(It.IsAny<UserRandomPostSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserRandomPostSettings, CancellationToken>((settings, _) => captured = settings)
            .ReturnsAsync(saved);

        var sut = CreateSut(ownerOid: "owner-1");

        var result = await sut.CreateAsync(request);

        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(StatusCodes.Status201Created);
        createdResult.ActionName.Should().Be(nameof(UserRandomPostSettingsController.GetAsync));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(10);
        createdResult.Value.Should().BeEquivalentTo(saved, options => options.ExcludingMissingMembers());
        captured.Should().NotBeNull();
        captured!.CreatedByEntraOid.Should().Be("owner-1");
        _onboardingManagerMock.Verify(m => m.RecalculateAsync("owner-1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        _managerMock.Setup(m => m.GetByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync((UserRandomPostSettings?)null);

        var sut = CreateSut();

        var result = await sut.UpdateAsync(11, new UpdateUserRandomPostSettingsRequest());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateAsync_WhenItemBelongsToAnotherUser_ReturnsForbid()
    {
        _managerMock.Setup(m => m.GetByIdAsync(12, It.IsAny<CancellationToken>())).ReturnsAsync(BuildSettings(12, ownerOid: "other-owner"));

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        var result = await sut.UpdateAsync(12, new UpdateUserRandomPostSettingsRequest { IsActive = false });

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateAsync_WhenOptionalFieldsAreOmitted_KeepsExistingValues()
    {
        var existing = BuildSettings(13);
        var saved = BuildSettings(13);
        saved.IsActive = false;

        UserRandomPostSettings? captured = null;
        _managerMock.Setup(m => m.GetByIdAsync(13, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _managerMock.Setup(m => m.SaveAsync(It.IsAny<UserRandomPostSettings>(), It.IsAny<CancellationToken>()))
            .Callback<UserRandomPostSettings, CancellationToken>((settings, _) => captured = settings)
            .ReturnsAsync(saved);

        var sut = CreateSut();

        var result = await sut.UpdateAsync(13, new UpdateUserRandomPostSettingsRequest { IsActive = false });

        result.Result.Should().BeOfType<OkObjectResult>();
        captured.Should().NotBeNull();
        captured!.SocialMediaPlatformId.Should().Be(existing.SocialMediaPlatformId);
        captured.CronExpression.Should().Be(existing.CronExpression);
        captured.ExcludedCategories.Should().BeEquivalentTo(existing.ExcludedCategories);
        captured.IsActive.Should().BeFalse();
        _onboardingManagerMock.Verify(m => m.RecalculateAsync(existing.CreatedByEntraOid, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenItemDoesNotExist_ReturnsNotFound()
    {
        _managerMock.Setup(m => m.GetByIdAsync(14, It.IsAny<CancellationToken>())).ReturnsAsync((UserRandomPostSettings?)null);

        var sut = CreateSut();

        var result = await sut.DeleteAsync(14);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteAsync_WhenItemBelongsToAnotherUser_ReturnsForbid()
    {
        _managerMock.Setup(m => m.GetByIdAsync(15, It.IsAny<CancellationToken>())).ReturnsAsync(BuildSettings(15, ownerOid: "other-owner"));

        var sut = CreateSut(ownerOid: "owner-oid-12345");

        var result = await sut.DeleteAsync(15);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task DeleteAsync_WhenDeleteSucceeds_ReturnsNoContent()
    {
        var settings = BuildSettings(16, ownerOid: "owner-2");
        _managerMock.Setup(m => m.GetByIdAsync(16, It.IsAny<CancellationToken>())).ReturnsAsync(settings);
        _managerMock.Setup(m => m.DeleteAsync(16, "owner-2", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut(ownerOid: "owner-2");

        var result = await sut.DeleteAsync(16);

        result.Should().BeOfType<NoContentResult>();
        _managerMock.Verify(m => m.DeleteAsync(16, "owner-2", It.IsAny<CancellationToken>()), Times.Once);
        _onboardingManagerMock.Verify(m => m.RecalculateAsync("owner-2", It.IsAny<CancellationToken>()), Times.Once);
    }
}
