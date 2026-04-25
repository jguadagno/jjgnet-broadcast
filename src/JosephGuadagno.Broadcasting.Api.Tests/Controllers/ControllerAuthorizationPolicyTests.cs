using System.Reflection;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
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
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController) },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController) },
        { typeof(YouTubeSourcesController), nameof(YouTubeSourcesController) }
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
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemsAsync), AuthorizationPolicyNames.RequireViewer },
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
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.SaveAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.DeleteAsync), AuthorizationPolicyNames.RequireAdministrator },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAllAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.UpdateAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(YouTubeSourcesController), nameof(YouTubeSourcesController.GetYouTubeSourcesAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(YouTubeSourcesController), nameof(YouTubeSourcesController.GetYouTubeSourceAsync), AuthorizationPolicyNames.RequireViewer },
        { typeof(YouTubeSourcesController), nameof(YouTubeSourcesController.CreateYouTubeSourceAsync), AuthorizationPolicyNames.RequireContributor },
        { typeof(YouTubeSourcesController), nameof(YouTubeSourcesController.DeleteYouTubeSourceAsync), AuthorizationPolicyNames.RequireAdministrator },
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
