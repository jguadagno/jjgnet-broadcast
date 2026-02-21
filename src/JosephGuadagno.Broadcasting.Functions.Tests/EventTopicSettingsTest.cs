using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Functions.Tests;

[Trait("Category", "Integration")]
public class EventTopicSettingsTest(IEventPublisherSettings eventPublisherSettings)
{
    [Fact(Skip = "Integration test - requires external services")]
    public void ShouldHaveTopicSettings()
    {
        Assert.NotEmpty(eventPublisherSettings.TopicEndpointSettings);
    }

    [Fact(Skip = "Integration test - requires external services")]
    public void ShouldHaveMoreThanOneTopic()
    {
        Assert.True(eventPublisherSettings.TopicEndpointSettings.Count > 1);
    }

}