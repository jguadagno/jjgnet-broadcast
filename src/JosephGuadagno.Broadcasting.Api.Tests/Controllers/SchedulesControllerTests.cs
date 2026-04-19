using System.Security.Claims;
using AutoMapper;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Api.Tests.Helpers;
using JosephGuadagno.Broadcasting.Domain;
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

    // Use the assembly-wide shared mapper to avoid AutoMapper profile-registry races
    // when xUnit runs test classes in parallel.  See ApiTestMapper for details.
    private static readonly IMapper _mapper = ApiTestMapper.Instance;

    public SchedulesControllerTests()
    {
        _scheduledItemManagerMock = new Mock<IScheduledItemManager>();
        _loggerMock = new Mock<ILogger<SchedulesController>>();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private SchedulesController CreateSut(string scopeClaimValue, string ownerOid = "owner-oid-12345", bool isSiteAdmin = false)
    {
        var controller = new SchedulesController(_scheduledItemManagerMock.Object, _loggerMock.Object, _mapper)
        {
            ControllerContext = ApiControllerTestHelpers.CreateControllerContext(scopeClaimValue, ownerOid, isSiteAdmin),
            ProblemDetailsFactory = new TestProblemDetailsFactory()
        };
        return controller;
    }

    private static ScheduledItem BuildScheduledItem(int id = 1, string oid = "owner-oid-12345")=> new()
    {
        Id = id,
        ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
        ItemPrimaryKey = id * 10,
        Message = $"Check out item {id}!",
        SendOnDateTime = DateTimeOffset.UtcNow.AddDays(id),
        MessageSent = false,
        // Must match the ownerOid set in CreateControllerContext so ownership checks pass.
        CreatedByEntraOid = oid
    };

    private static ScheduledItemRequest BuildScheduledItemRequest(int id = 1) => new()
    {
        ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
        ItemPrimaryKey = id * 10,
        Message = $"Check out item {id}!",
        SendOnDateTime = DateTimeOffset.UtcNow.AddDays(id)
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
        _scheduledItemManagerMock.Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = items.Count });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(items, opts => opts.ExcludingMissingMembers());
        result.Value!.TotalCount.Should().Be(2);
        _scheduledItemManagerMock.Verify(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsAsync_WhenNoItemsExist_ReturnsEmptyList()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().BeEmpty();
        result.Value!.TotalCount.Should().Be(0);
        _scheduledItemManagerMock.Verify(m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once);
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
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(item, opts => opts.ExcludingMissingMembers());
        _scheduledItemManagerMock.Verify(m => m.GetAsync(7), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemAsync_WhenItemNotFound_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetAsync(99)).Returns(Task.FromResult<ScheduledItem?>(null));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetAsync(99), Times.Once);
    }

    // -------------------------------------------------------------------------
    // CreateScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateScheduledItemAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = new ScheduledItemRequest { ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources, ItemPrimaryKey = 0, Message = "Test", SendOnDateTime = DateTimeOffset.UtcNow.AddDays(1) };
        var sut = CreateSut(Domain.Scopes.Schedules.All);
        sut.ModelState.AddModelError("Message", "Message is required");

        // Act
        var result = await sut.CreateScheduledItemAsync(request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task CreateScheduledItemAsync_WhenSaveSucceeds_ReturnsCreatedAtActionWithItem()
    {
        // Arrange
        var request = new ScheduledItemRequest
        {
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 100,
            Message = "New scheduled post",
            SendOnDateTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        var savedItem = BuildScheduledItem(33);
        savedItem.Message = request.Message;
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(OperationResult<ScheduledItem>.Success(savedItem));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.CreateScheduledItemAsync(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.ActionName.Should().Be(nameof(SchedulesController.GetScheduledItemAsync));
        createdResult.RouteValues.Should().ContainKey("scheduledItemId").WhoseValue.Should().Be(33);
        createdResult.Value.Should().BeEquivalentTo(savedItem, opts => opts.ExcludingMissingMembers());
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Once);
    }

    [Fact]
    public async Task CreateScheduledItemAsync_WhenCalled_StampsCreatedByEntraOidFromClaims()
    {
        // Arrange
        var request = BuildScheduledItemRequest(4);
        ScheduledItem? capturedItem = null;
        _scheduledItemManagerMock
            .Setup(m => m.SaveAsync(It.IsAny<ScheduledItem>()))
            .Callback<ScheduledItem, CancellationToken>((item, _) => capturedItem = item)
            .ReturnsAsync(OperationResult<ScheduledItem>.Success(BuildScheduledItem(4, oid: "owner-1")));

        var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "owner-1");

        // Act
        var result = await sut.CreateScheduledItemAsync(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        capturedItem.Should().NotBeNull();
        capturedItem!.CreatedByEntraOid.Should().Be("owner-1");
    }

    [Fact]
    public async Task CreateScheduledItemAsync_WhenSaveFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = new ScheduledItemRequest
        {
            ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedSources,
            ItemPrimaryKey = 10,
            Message = "Check out item!",
            SendOnDateTime = DateTimeOffset.UtcNow.AddDays(1)
        };
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(OperationResult<ScheduledItem>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.CreateScheduledItemAsync(request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // UpdateScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task UpdateScheduledItemAsync_WhenModelStateIsInvalid_ReturnsBadRequest()
    {
        // Arrange
        var request = BuildScheduledItemRequest(5);
        var sut = CreateSut(Domain.Scopes.Schedules.All);
        sut.ModelState.AddModelError("Message", "Message is required");

        // Act
        var result = await sut.UpdateScheduledItemAsync(5, request);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateScheduledItemAsync_WhenUpdateSucceeds_ReturnsOkWithItem()
    {
        // Arrange
        var request = BuildScheduledItemRequest(5);
        // GetAsync is called first to load the existing item for the ownership check.
        _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(BuildScheduledItem(5));
        var savedItem = BuildScheduledItem(5);
        savedItem.Message = "Updated message";
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(OperationResult<ScheduledItem>.Success(savedItem));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.UpdateScheduledItemAsync(5, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(savedItem, opts => opts.ExcludingMissingMembers());
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduledItemAsync_WhenUpdateFails_ReturnsInternalServerError()
    {
        // Arrange
        var request = BuildScheduledItemRequest(5);
        // GetAsync is called first to load the existing item for the ownership check.
        _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(BuildScheduledItem(5));
        _scheduledItemManagerMock.Setup(m => m.SaveAsync(It.IsAny<ScheduledItem>())).ReturnsAsync(OperationResult<ScheduledItem>.Failure("Save failed"));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.UpdateScheduledItemAsync(5, request);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Once);
    }

    // -------------------------------------------------------------------------
    // DeleteScheduledItemAsync
    // -------------------------------------------------------------------------

    [Fact]
    public async Task DeleteScheduledItemAsync_WhenItemExists_ReturnsNoContent()
    {
        // Arrange
        // GetAsync is called first to load the existing item for the ownership check.
        _scheduledItemManagerMock.Setup(m => m.GetAsync(1)).ReturnsAsync(BuildScheduledItem(1));
        _scheduledItemManagerMock.Setup(m => m.DeleteAsync(1)).ReturnsAsync(OperationResult<bool>.Success(true));

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
        // The controller now fetches the item first (ownership check).  When GetAsync
        // returns null it short-circuits with NotFound — DeleteAsync is never invoked.
        _scheduledItemManagerMock.Setup(m => m.GetAsync(99)).Returns(Task.FromResult<ScheduledItem?>(null));

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.DeleteScheduledItemAsync(99);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetAsync(99), Times.Once);
        _scheduledItemManagerMock.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Security: non-owner → 403 ForbidResult
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetScheduledItemAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        // Item is owned by "owner-oid-12345"; the calling user has a different OID.
        var item = BuildScheduledItem(5, oid: "owner-oid-12345");
        _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(item);

        var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.GetScheduledItemAsync(5);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _scheduledItemManagerMock.Verify(m => m.GetAsync(5), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduledItemAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        var item = BuildScheduledItem(5, oid: "owner-oid-12345");
        var request = BuildScheduledItemRequest(5);
        _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(item);

        var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.UpdateScheduledItemAsync(5, request);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _scheduledItemManagerMock.Verify(m => m.SaveAsync(It.IsAny<ScheduledItem>()), Times.Never);
    }

    [Fact]
    public async Task DeleteScheduledItemAsync_WhenNonOwner_ReturnsForbid()
    {
        // Arrange
        var item = BuildScheduledItem(5, oid: "owner-oid-12345");
        _scheduledItemManagerMock.Setup(m => m.GetAsync(5)).ReturnsAsync(item);

        var sut = CreateSut(Domain.Scopes.Schedules.All, ownerOid: "non-owner-oid-99999");

        // Act
        var result = await sut.DeleteScheduledItemAsync(5);

        // Assert
        result.Result.Should().BeOfType<ForbidResult>();
        _scheduledItemManagerMock.Verify(m => m.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Security: SiteAdmin list → unfiltered GetAll
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetScheduledItemsAsync_WhenSiteAdmin_CallsUnfilteredGetAll()
    {
        // Arrange
        var items = new List<ScheduledItem> { BuildScheduledItem(1) };
        // Set up the unfiltered overload (no ownerOid, first param is int page).
        _scheduledItemManagerMock
            .Setup(m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = items, TotalCount = items.Count });

        var sut = CreateSut(Domain.Scopes.Schedules.All, isSiteAdmin: true);

        // Act
        var result = await sut.GetScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(1);

        // Unfiltered overload must be invoked exactly once …
        _scheduledItemManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<int>(), It.IsAny<int>()),
            Times.Once);
        // … and the owner-filtered overload must never be called.
        _scheduledItemManagerMock.Verify(
            m => m.GetAllAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()),
            Times.Never);
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
        _scheduledItemManagerMock.Setup(m => m.GetUnsentScheduledItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = unsentItems, TotalCount = unsentItems.Count });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUnsentScheduledItemsAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(unsentItems, opts => opts.ExcludingMissingMembers());
        result.Value!.TotalCount.Should().Be(2);
        _scheduledItemManagerMock.Verify(m => m.GetUnsentScheduledItemsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetUnsentScheduledItemsAsync_WhenNoUnsentItems_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetUnsentScheduledItemsAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUnsentScheduledItemsAsync();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetUnsentScheduledItemsAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
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
        _scheduledItemManagerMock.Setup(m => m.GetScheduledItemsToSendAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = upcomingItems, TotalCount = upcomingItems.Count });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsToSendAsync();

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(upcomingItems, opts => opts.ExcludingMissingMembers());
        result.Value!.TotalCount.Should().Be(2);
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsToSendAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetScheduledItemsToSendAsync_WhenNoItemsToSend_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock.Setup(m => m.GetScheduledItemsToSendAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetScheduledItemsToSendAsync();

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsToSendAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
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
            .Setup(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 8, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = calendarItems, TotalCount = calendarItems.Count });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUpcomingScheduledItemsForCalendarMonthAsync(2025, 8);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value!.Items.Should().HaveCount(2);
        result.Value!.Items.Should().BeEquivalentTo(calendarItems, opts => opts.ExcludingMissingMembers());
        result.Value!.TotalCount.Should().Be(2);
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 8, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task GetUpcomingScheduledItemsForCalendarMonthAsync_WhenNoItemsForMonth_ReturnsNotFound()
    {
        // Arrange
        _scheduledItemManagerMock
            .Setup(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 1, It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new PagedResult<ScheduledItem> { Items = new List<ScheduledItem>(), TotalCount = 0 });

        var sut = CreateSut(Domain.Scopes.Schedules.All);

        // Act
        var result = await sut.GetUpcomingScheduledItemsForCalendarMonthAsync(2025, 1);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
        _scheduledItemManagerMock.Verify(m => m.GetScheduledItemsByCalendarMonthAsync(2025, 1, It.IsAny<int>(), It.IsAny<int>()), Times.Once);
    }
}
