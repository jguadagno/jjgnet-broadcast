using FluentAssertions;
using JosephGuadagno.Broadcasting.Domain.Constants;
using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;
using Moq;

namespace JosephGuadagno.Broadcasting.Managers.Tests;

public class UserEventPublisherMappingManagerTests
{
    private readonly Mock<IUserEventPublisherMappingDataStore> _dataStore = new();
    private readonly UserEventPublisherMappingManager _sut;

    public UserEventPublisherMappingManagerTests()
    {
        _sut = new UserEventPublisherMappingManager(_dataStore.Object);
    }

    [Fact]
    public async Task GetByUserAndEventTypeAsync_DelegatesToDataStore()
    {
        const string ownerOid = "owner-1";
        var expected = new List<UserEventPublisherMapping>
        {
            new() { CreatedByEntraOid = ownerOid, EventType = MessageTemplates.MessageTypes.NewYouTubeItem, SocialMediaPlatformId = 2 }
        };

        _dataStore.Setup(d => d.GetByUserAndEventTypeAsync(ownerOid, MessageTemplates.MessageTypes.NewYouTubeItem, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _sut.GetByUserAndEventTypeAsync(ownerOid, MessageTemplates.MessageTypes.NewYouTubeItem);

        result.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task SaveAsync_DelegatesToDataStore()
    {
        var mapping = new UserEventPublisherMapping
        {
            CreatedByEntraOid = "owner-1",
            EventType = MessageTemplates.MessageTypes.NewSyndicationFeedItem,
            SocialMediaPlatformId = 1
        };

        _dataStore.Setup(d => d.SaveAsync(mapping, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mapping);

        var result = await _sut.SaveAsync(mapping);

        result.Should().Be(mapping);
    }

    [Fact]
    public void SaveAsync_ThrowsForUnsupportedEventType()
    {
        var mapping = new UserEventPublisherMapping
        {
            CreatedByEntraOid = "owner-1",
            EventType = "UnsupportedEvent",
            SocialMediaPlatformId = 1
        };

        var action = async () => await _sut.SaveAsync(mapping);

        action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*supported message type*");
    }

    [Fact]
    public async Task DeleteAsync_DelegatesToDataStore()
    {
        _dataStore.Setup(d => d.DeleteAsync(42, "owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(42, "owner-1");

        result.Should().BeTrue();
    }
}
