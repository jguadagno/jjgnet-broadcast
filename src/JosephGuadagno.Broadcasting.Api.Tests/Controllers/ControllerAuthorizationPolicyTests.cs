using System.Reflection;
using FluentAssertions;
using JosephGuadagno.Broadcasting.Api.Controllers;
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
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController) }
    };

    public static TheoryData<Type, string, string> ActionPolicies => new()
    {
        { typeof(EngagementsController), nameof(EngagementsController.GetEngagementsAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.GetEngagementAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.CreateEngagementAsync), "RequireContributor" },
        { typeof(EngagementsController), nameof(EngagementsController.UpdateEngagementAsync), "RequireContributor" },
        { typeof(EngagementsController), nameof(EngagementsController.DeleteEngagementAsync), "RequireAdministrator" },
        { typeof(EngagementsController), nameof(EngagementsController.GetTalksForEngagementAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.CreateTalkAsync), "RequireContributor" },
        { typeof(EngagementsController), nameof(EngagementsController.UpdateTalkAsync), "RequireContributor" },
        { typeof(EngagementsController), nameof(EngagementsController.GetTalkAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.DeleteTalkAsync), "RequireAdministrator" },
        { typeof(EngagementsController), nameof(EngagementsController.GetPlatformsForEngagementAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.GetPlatformForEngagementAsync), "RequireViewer" },
        { typeof(EngagementsController), nameof(EngagementsController.AddPlatformToEngagementAsync), "RequireContributor" },
        { typeof(EngagementsController), nameof(EngagementsController.RemovePlatformFromEngagementAsync), "RequireAdministrator" },
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemsAsync), "RequireViewer" },
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemAsync), "RequireViewer" },
        { typeof(SchedulesController), nameof(SchedulesController.CreateScheduledItemAsync), "RequireContributor" },
        { typeof(SchedulesController), nameof(SchedulesController.UpdateScheduledItemAsync), "RequireContributor" },
        { typeof(SchedulesController), nameof(SchedulesController.DeleteScheduledItemAsync), "RequireAdministrator" },
        { typeof(SchedulesController), nameof(SchedulesController.GetUnsentScheduledItemsAsync), "RequireViewer" },
        { typeof(SchedulesController), nameof(SchedulesController.GetScheduledItemsToSendAsync), "RequireViewer" },
        { typeof(SchedulesController), nameof(SchedulesController.GetUpcomingScheduledItemsForCalendarMonthAsync), "RequireViewer" },
        { typeof(SchedulesController), nameof(SchedulesController.GetOrphanedScheduledItemsAsync), "RequireViewer" },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.GetAllAsync), "RequireViewer" },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.GetAsync), "RequireViewer" },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.CreateAsync), "RequireContributor" },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.UpdateAsync), "RequireContributor" },
        { typeof(SocialMediaPlatformsController), nameof(SocialMediaPlatformsController.DeleteAsync), "RequireAdministrator" },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.GetAllAsync), "RequireViewer" },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.GetAsync), "RequireViewer" },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.SaveAsync), "RequireContributor" },
        { typeof(UserPublisherSettingsController), nameof(UserPublisherSettingsController.DeleteAsync), "RequireAdministrator" },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAllAsync), "RequireViewer" },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.GetAsync), "RequireViewer" },
        { typeof(MessageTemplatesController), nameof(MessageTemplatesController.UpdateAsync), "RequireContributor" }
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
