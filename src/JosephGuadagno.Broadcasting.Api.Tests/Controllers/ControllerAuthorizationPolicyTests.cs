using System.Reflection;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
using JosephGuadagno.Broadcasting.Api.Controllers.Collectors;
using JosephGuadagno.Broadcasting.Api.Controllers.Publishers;
using JosephGuadagno.Broadcasting.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace JosephGuadagno.Broadcasting.Api.Tests.Controllers;

public class ControllerAuthorizationPolicyTests
{
    public static TheoryData<Type, string> ControllerTypes => new()
    {
        { typeof(EngagementsController), nameof(EngagementsController) },
        { typeof(SchedulesController), nameof(SchedulesController) },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController) },
        { typeof(PublishersController), nameof(PublishersController) },
        { typeof(CollectorsController), nameof(CollectorsController) },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController) },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController) },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController) },
        { typeof(BlueskySettingsController), nameof(BlueskySettingsController) },
        { typeof(TwitterSettingsController), nameof(TwitterSettingsController) },
        { typeof(LinkedInSettingsController), nameof(LinkedInSettingsController) },
        { typeof(FacebookSettingsController), nameof(FacebookSettingsController) },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController) },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController) },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController) },
        { typeof(YouTubeItemsController), nameof(YouTubeItemsController) }
    };

    public static TheoryData<Type, string, string> ActionPolicies => new()
    {
        { typeof(EngagementsController), nameof(EngagementsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.GetEngagementAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.CreateEngagementAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(EngagementsController), nameof(EngagementsController.UpdateEngagementAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(EngagementsController), nameof(EngagementsController.DeleteEngagementAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(EngagementsController), nameof(EngagementsController.GetTalksForEngagementAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.CreateTalkAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(EngagementsController), nameof(EngagementsController.UpdateTalkAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(EngagementsController), nameof(EngagementsController.GetTalkAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.DeleteTalkAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(EngagementsController), nameof(EngagementsController.GetPlatformsForEngagementAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.GetPlatformForEngagementAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(EngagementsController), nameof(EngagementsController.AddPlatformToEngagementAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(EngagementsController), nameof(EngagementsController.RemovePlatformFromEngagementAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(SchedulesController), nameof(SchedulesController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SchedulesController), nameof(SchedulesController.CreateScheduledItemAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(SchedulesController), nameof(SchedulesController.UpdateScheduledItemAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(SchedulesController), nameof(SchedulesController.DeleteScheduledItemAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(SchedulesController), nameof(SchedulesController.GetUnsentScheduledItemsAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemsToSendAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SchedulesController), nameof(SchedulesController.GetUpcomingScheduledItemsForCalendarMonthAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SchedulesController), nameof(SchedulesController.GetOrphanedScheduledItemsAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.CreateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.DeleteAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(PublishersController), nameof(PublishersController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorsController), nameof(CollectorsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorYouTubeSettingsController), nameof(CollectorYouTubeSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorFeedSourceSettingsController), nameof(CollectorFeedSourceSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(CollectorSpeakingEngagementSettingsController), nameof(CollectorSpeakingEngagementSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(BlueskySettingsController), nameof(BlueskySettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(BlueskySettingsController), nameof(BlueskySettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(BlueskySettingsController), nameof(BlueskySettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(TwitterSettingsController), nameof(TwitterSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(TwitterSettingsController), nameof(TwitterSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(TwitterSettingsController), nameof(TwitterSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(LinkedInSettingsController), nameof(LinkedInSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(LinkedInSettingsController), nameof(LinkedInSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(LinkedInSettingsController), nameof(LinkedInSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(FacebookSettingsController), nameof(FacebookSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(FacebookSettingsController), nameof(FacebookSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(FacebookSettingsController), nameof(FacebookSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController.CreateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserRandomPostSettingsController), nameof(UserRandomPostSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController.CreateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserEventPublisherMappingController), nameof(UserEventPublisherMappingController.DeleteAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(YouTubeItemsController), nameof(YouTubeItemsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(YouTubeItemsController), nameof(YouTubeItemsController.GetYouTubeItemAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(YouTubeItemsController), nameof(YouTubeItemsController.CreateYouTubeItemAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(YouTubeItemsController), nameof(YouTubeItemsController.DeleteYouTubeItemAsync), AuthorizationPolicyNames.RequireAdministrator },
    };

    [Theory]
    [MemberData(nameof(ControllerTypes))]
    public void Controllers_KeepClassLevelAuthorizeAttribute(Type controllerType, string controllerName)
    {
        var authorizeAttributes = controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: false);

        authorizeAttributes.Should().ContainSingle(attribute => string.IsNullOrEmpty(attribute.Policy), $"{controllerName} should still require authentication at the controller level");
    }

    [Theory]
    [MemberData(nameof(ActionPolicies))]
    public void Actions_UseExpectedPolicy(Type controllerType, string actionName, string expectedPolicy)
    {
        var action = controllerType.GetMethod(actionName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        action.Should().NotBeNull();
        action!
            .GetCustomAttributes<AuthorizeAttribute>(inherit: false)
            .Should()
            .ContainSingle(attribute => attribute.Policy == expectedPolicy);
    }
}
