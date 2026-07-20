using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JosephGuadagno.Broadcasting.Domain;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using JosephGuadagno.Broadcasting.Functions.Distributors;
using JosephGuadagno.Broadcasting.Functions.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JosephGuadagno.Broadcasting.Functions.Tests.Distributors;

public class ScheduledItemsTests
{
    private readonly Mock<IScheduledItemManager> _scheduledItemManager;
    private readonly Mock<IScheduledItemEventDistributor> _scheduledItemEventDispatcher;
    private readonly Mock<IFeedCheckManager> _feedCheckManager;
    private readonly ScheduledItems _sut;

    public ScheduledItemsTests()
    {
        _scheduledItemManager = new Mock<IScheduledItemManager>();
        _scheduledItemEventDispatcher = new Mock<IScheduledItemEventDistributor>();
        _feedCheckManager = new Mock<IFeedCheckManager>();
        _feedCheckManager
            .Setup(m => m.SaveAsync(It.IsAny<FeedCheck>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OperationResult<FeedCheck>.Success(new FeedCheck()));

        _sut = new ScheduledItems(
            _scheduledItemManager.Object,
            _scheduledItemEventDispatcher.Object,
            _feedCheckManager.Object,
            NullLogger<ScheduledItems>.Instance);
    }

    private static ScheduledItem BuildScheduledItem(int id, string? ownerOid = "scheduled-owner-oid") => new()
    {
        Id = id,
        ItemType = Domain.Enums.ScheduledItemType.SyndicationFeedItems,
        ItemPrimaryKey = id * 10,
        Message = "scheduled message",
        SendOnDateTime = DateTimeOffset.UtcNow,
        CreatedByEntraOid = ownerOid,
    };

    [Fact]
    public async Task RunAsync_WhenNoScheduledItemsFound_DoesNotPublishOrMarkSent()
    {
        _scheduledItemManager
            .Setup(m => m.GetScheduledItemsToSendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduledItem>());

        await _sut.RunAsync(null!);

        _scheduledItemEventDispatcher.Verify(m => m.DispatchAsync(It.IsAny<ScheduledItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _scheduledItemManager.Verify(m => m.SentScheduledItemAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _feedCheckManager.Verify(m => m.SaveAsync(It.IsAny<FeedCheck>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenScheduledItemsExist_PublishesMarksSentAndUpdatesFeedCheck()
    {
        var scheduledItem = BuildScheduledItem(1);
        _scheduledItemManager
            .Setup(m => m.GetScheduledItemsToSendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduledItem> { scheduledItem });
        _feedCheckManager
            .Setup(m => m.GetByNameAsync(ConfigurationFunctionNames.DistributorsScheduledItems, scheduledItem.CreatedByEntraOid!, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedCheck?)null);
        _scheduledItemManager
            .Setup(m => m.SentScheduledItemAsync(scheduledItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(null!);

        _scheduledItemEventDispatcher.Verify(m => m.DispatchAsync(
            It.Is<ScheduledItem>(i => i.Id == scheduledItem.Id),
            It.IsAny<CancellationToken>()), Times.Once);
        _scheduledItemManager.Verify(m => m.SentScheduledItemAsync(scheduledItem.Id, It.IsAny<CancellationToken>()), Times.Once);
        _feedCheckManager.Verify(m => m.SaveAsync(
            It.Is<FeedCheck>(f => f.Name == ConfigurationFunctionNames.DistributorsScheduledItems && f.EntraOId == scheduledItem.CreatedByEntraOid),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_WhenOneScheduledItemPublishFails_ContinuesProcessingRemainingItems()
    {
        var failedItem = BuildScheduledItem(1);
        var successfulItem = BuildScheduledItem(2);

        _scheduledItemManager
            .Setup(m => m.GetScheduledItemsToSendAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScheduledItem> { failedItem, successfulItem });
        _feedCheckManager
            .Setup(m => m.GetByNameAsync(ConfigurationFunctionNames.DistributorsScheduledItems, failedItem.CreatedByEntraOid!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedCheck
            {
                Id = 1,
                Name = ConfigurationFunctionNames.DistributorsScheduledItems,
                EntraOId = failedItem.CreatedByEntraOid!,
                LastCheckedFeed = DateTimeOffset.UtcNow.AddDays(-1),
                LastItemAddedOrUpdated = DateTimeOffset.MinValue,
                LastUpdatedOn = DateTimeOffset.UtcNow.AddDays(-1),
            });
        _scheduledItemEventDispatcher
            .Setup(m => m.DispatchAsync(It.Is<ScheduledItem>(i => i.Id == failedItem.Id), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("queue failure"));
        _scheduledItemManager
            .Setup(m => m.SentScheduledItemAsync(successfulItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _sut.RunAsync(null!);

        _scheduledItemManager.Verify(m => m.SentScheduledItemAsync(failedItem.Id, It.IsAny<CancellationToken>()), Times.Never);
        _scheduledItemManager.Verify(m => m.SentScheduledItemAsync(successfulItem.Id, It.IsAny<CancellationToken>()), Times.Once);
        _feedCheckManager.Verify(m => m.SaveAsync(It.IsAny<FeedCheck>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
