using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

public class EventTopicSettingsTest(IEventPublisherSettings eventPublisherSettings)
{
    [Fact]
    public void ShouldHaveTopicSettings()
    {
        Assert.NotEmpty(eventPublisherSettings.TopicEndpointSettings);
    }

    [Fact]
    public void ShouldHaveMoreThanOneTopic()
    {
        Assert.True(eventPublisherSettings.TopicEndpointSettings.Count > 1);
    }

}