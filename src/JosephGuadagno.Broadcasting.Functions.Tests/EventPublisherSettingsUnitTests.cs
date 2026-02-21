using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

/// <summary>
/// Unit tests for EventPublisherSettings domain model
/// </summary>
public class EventPublisherSettingsUnitTests
{
    [Fact]
    public void EventPublisherSettings_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange
        var topicSettings = new System.Collections.Generic.List<ITopicEndpointSettings>
        {
            new TopicEndpointSettings
            {
                TopicName = "TestTopic1",
                Endpoint = "https://example.com/topic1",
                Key = "key1"
            },
            new TopicEndpointSettings
            {
                TopicName = "TestTopic2",
                Endpoint = "https://example.com/topic2",
                Key = "key2"
            }
        };

        // Act
        var settings = new EventPublisherSettings
        {
            TopicEndpointSettings = topicSettings
        };

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.TopicEndpointSettings);
        Assert.Equal(2, settings.TopicEndpointSettings.Count);
        Assert.Equal("TestTopic1", settings.TopicEndpointSettings[0].TopicName);
        Assert.Equal("https://example.com/topic1", settings.TopicEndpointSettings[0].Endpoint);
        Assert.Equal("key1", settings.TopicEndpointSettings[0].Key);
        Assert.Equal("TestTopic2", settings.TopicEndpointSettings[1].TopicName);
        Assert.Equal("https://example.com/topic2", settings.TopicEndpointSettings[1].Endpoint);
        Assert.Equal("key2", settings.TopicEndpointSettings[1].Key);
    }

    [Fact]
    public void EventPublisherSettings_WithEmptyList_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var settings = new EventPublisherSettings
        {
            TopicEndpointSettings = new System.Collections.Generic.List<ITopicEndpointSettings>()
        };

        // Assert
        Assert.NotNull(settings);
        Assert.NotNull(settings.TopicEndpointSettings);
        Assert.Empty(settings.TopicEndpointSettings);
    }

    [Fact]
    public void TopicEndpointSettings_WithValidData_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var topicSettings = new TopicEndpointSettings
        {
            TopicName = "TestTopic",
            Endpoint = "https://example.com/test",
            Key = "testKey123"
        };

        // Assert
        Assert.NotNull(topicSettings);
        Assert.Equal("TestTopic", topicSettings.TopicName);
        Assert.Equal("https://example.com/test", topicSettings.Endpoint);
        Assert.Equal("testKey123", topicSettings.Key);
    }

    [Fact]
    public void TopicEndpointSettings_ShouldBeModifiable()
    {
        // Arrange
        var topicSettings = new TopicEndpointSettings
        {
            TopicName = "OriginalName",
            Endpoint = "https://original.com",
            Key = "originalKey"
        };

        // Act
        topicSettings.TopicName = "ModifiedName";
        topicSettings.Endpoint = "https://modified.com";
        topicSettings.Key = "modifiedKey";

        // Assert
        Assert.Equal("ModifiedName", topicSettings.TopicName);
        Assert.Equal("https://modified.com", topicSettings.Endpoint);
        Assert.Equal("modifiedKey", topicSettings.Key);
    }
}
