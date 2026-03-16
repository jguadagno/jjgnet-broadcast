using System.Security.Claims;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class SchedulesControllerTests
{
    private readonly Mock<IScheduledItemManager> _scheduledItemManagerMock;
    private readonly Mock<ILogger<SchedulesController>> _loggerMock;

    public SchedulesControllerTests()
    {
        _scheduledItemManagerMock = new Mock<IScheduledItemManager>();
        _loggerMock = new Mock<ILogger<SchedulesController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SchedulesController CreateSut(string scopeClaimValue)
    {
        var controller = new SchedulesController(_scheduledItemManagerMock.Object, _loggerMock.Object)
        {
            ControllerContext = CreateControllerContext(scopeClaimValue),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    /// <summary>
    /// Builds an HttpContext whose <see cref="ClaimsPrincipal"/> carries the given OAuth
    /// scope so that <c>HttpContext.VerifyUserHasAnyAcceptedScope</c> succeeds.
    /// Both the short "scp" claim and the full URI claim type are set for maximum
    /// compatibility with different versions of Microsoft.Identity.Web.
    /// </summary>
    private static ControllerContext CreateControllerContext(string scopeClaimValue)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scp", scopeClaimValue),
            new Claim("http://schemas.microsoft.com/identity/claims/scope", scopeClaimValue)
        ], "TestAuthentication"));

        var httpContext = new DefaultHttpContext { User = user };
        return new ControllerContext { HttpContext = httpContext };
    }

    private static ScheduledItem BuildScheduledItem(int id = 1) => new()
    {
        Id = id,
        ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
        ItemPrimaryKey = id * 10,
        Message = $"Check out item {id}!",
        SendOnDateTime = DateTimeOffset.UtcNow.AddDays(id),
        MessageSent = false
    };

    // -------------------------------------------------------------------------
    // GetScheduledItemsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetScheduledItemsAsync_WhenCalled_ReturnsAllScheduledItems()
    {
        // Arrange
        var items = new List<ScheduledItem>
        {
            BuildScheduledItem(1),
            BuildScheduledItem(2)
        };
        _scheduledItemManagerMock.Setup(m => m.GetAllAsync()).ReturnsAsync(items);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(items);
        _scheduledItemManagerMock.Verify(m => m.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsAsync_WhenNoItemsExist_ReturnsEmptyList()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetAllAsync()).ReturnsAsync(new List<ScheduledItem>());

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
        _scheduledItemManagerMock.Verify(m => m.GetAllAsync(), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetScheduledItemAsync_WhenItemExists_ReturnsScheduledItem()
    {
        // Arrange
        var item = BuildScheduledItem(7);
        _scheduledItemManagerMock.Setup(m => m.GetAsync(7)).ReturnsAsync(item);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemAsync(7);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().Be(item);
        _scheduledItemManagerMock.Verify(m => m.GetAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemAsync_WhenItemNotFound_ReturnsNullValue()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetAsync(99)).Returns(Task.FromResult<ScheduledItem?>(null));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemAsync(99);

        // Assert
        result.Value.Should().BeNull();
        _scheduledItemManagerMock.Verify(m => m.GetAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // SaveScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveScheduledItemAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var item = new ScheduledItem { Id = 1 };
        var sut = CreateSut(Domain.Scopes.Schedules.All);
        sut.ModelState.AddModelError("Message", "Message is required");

        // Act
        var result = await sut.SaveScheduledItemAsync(item);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task SaveScheduledItemAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithItem()
    {
        // Arrange
        var item = new ScheduledItem
        {
            Id = 0,
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 100,
            Message = "New scheduled post",
            SendOnDateTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var savedItem = BuildScheduledItem(33);
        savedItem.Message = item.Message;
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(item)).ReturnsAsync(savedItem);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.SaveScheduledItemAsync(item);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SchedulesController.GetScheduledItemAsync));
        createdResult.RouteValues.Should().ContainKey("scheduledItemId").WhoseValue.Should().Be(33);
        createdResult.Value.Should().Be(savedItem);
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(item), Times.Once);
    }

    [Fact]
    public async Task SaveScheduledItemAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var item = BuildScheduledItem(5);
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(item)).Returns(Task.FromResult<ScheduledItem?>(null));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.SaveScheduledItemAsync(item);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(item), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteScheduledItemAsync_WhenItemExists_ReturnsNoContent()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.DeleteAsync(1)).ReturnsAsync(true);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.DeleteScheduledItemAsync(1);

        // Assert
        result.Result.Should().BeOfType<NoContentResult>();
        _scheduledItemManagerMock.Verify(m => m.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteScheduledItemAsync_WhenItemNotFound_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.DeleteAsync(99)).ReturnsAsync(false);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.DeleteScheduledItemAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.DeleteAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetUnsentScheduledItemsAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_WhenUnsentItemsExist_ReturnsItems()
    {
        // Arrange
        var unsentItems = new List<ScheduledItem>
        {
            BuildScheduledItem(1),
            BuildScheduledItem(2)
        };
        _scheduledItemManagerMock.Setup(m => m.GetUnsentScheduledItemsAsync()).ReturnsAsync(unsentItems);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUnsentScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(unsentItems);
        _scheduledItemManagerMock.Verify(m => m.GetUnsentScheduledItemsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_WhenNoUnsentItems_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetUnsentScheduledItemsAsync())
            .ReturnsAsync(new List<ScheduledItem>());

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUnsentScheduledItemsAsync();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetUnsentScheduledItemsAsync(), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetScheduledItemsToSendAsync (upcoming)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetScheduledItemsToSendAsync_WhenItemsExist_ReturnsUpcomingItems()
    {
        // Arrange
        var upcomingItems = new List<ScheduledItem>
        {
            BuildScheduledItem(3),
            BuildScheduledItem(4)
        };
        _scheduledItemManagerMock.Setup(m => m.GetScheduledItemsToSendAsync()).ReturnsAsync(upcomingItems);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsToSendAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(upcomingItems);
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsToSendAsync(), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsToSendAsync_WhenNoItemsToSend_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetScheduledItemsToSendAsync())
            .ReturnsAsync(new List<ScheduledItem>());

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsToSendAsync();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsToSendAsync(), Times.Once);
    }

    // -------------------------------------------------------------------------
    // GetUpcomingScheduledItemsForCalendarMonthAsync (calendar)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetUpcomingScheduledItemsForCalendarMonthAsync_WhenItemsExist_ReturnsItemsForMonth()
    {
        // Arrange
        var calendarItems = new List<ScheduledItem>
        {
            BuildScheduledItem(5),
            BuildScheduledItem(6)
        };
        _scheduledItemManagerMock
            .Setup(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 8))
            .ReturnsAsync(calendarItems);

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUpcomingScheduledItemsForCalendarMonthAsync(2025, 8);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Should().BeEquivalentTo(calendarItems);
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 8), Times.Once);
    }

    [Fact]
    public async Task GetUpcomingScheduledItemsForCalendarMonthAsync_WhenNoItemsForMonth_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock
            .Setup(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 1))
            .ReturnsAsync(new List<ScheduledItem>());

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUpcomingScheduledItemsForCalendarMonthAsync(2025, 1);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 1), Times.Once);
    }
}
