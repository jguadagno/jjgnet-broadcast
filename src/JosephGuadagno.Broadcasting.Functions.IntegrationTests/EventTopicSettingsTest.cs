using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.IntegrationTests;

[Trait("Category", "Integration")]
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
