using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Web.MappingProfiles;

/// <summary>
/// Maps the models to the view models.
/// </summary>
public class WebMappingProfile: Profile
{
    /// <summary>
    /// The constructors that do the mapping.
    /// </summary>
    public WebMappingProfile()
    {
        CreateMap<Models.EngagementViewModel, Domain.Models.Engagement>()
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore())
            .ForMember(destination => destination.SocialMediaPlatforms, options => options.Ignore());
        CreateMap<Models.TalkViewModel, Domain.Models.Talk>()
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore());
        CreateMap<Models.ScheduledItemViewModel, Domain.Models.ScheduledItem>()
            .ForMember(
                destination => destination.ItemType,
                options => options.MapFrom(source => source.ItemType))
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore())
            .ForMember(destination => destination.SocialMediaPlatformId, options => options.Ignore());
        CreateMap<Models.MessageTemplateViewModel, Domain.Models.MessageTemplate>()
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore())
            .ForMember(destination => destination.SocialMediaPlatformId, options => options.Ignore());

        CreateMap<Domain.Models.Engagement, Models.EngagementViewModel>()
            .ForMember(destination => destination.TimeZones, options => options.Ignore())
            .ForMember(destination => destination.SocialMediaPlatforms, options => options.Ignore());
        CreateMap<Domain.Models.EngagementSocialMediaPlatform, Models.EngagementSocialMediaPlatformViewModel>()
            .ForMember(destination => destination.PlatformName,
                options => options.MapFrom(source => source.SocialMediaPlatform != null ? source.SocialMediaPlatform.Name : null))
            .ForMember(destination => destination.PlatformIcon,
                options => options.MapFrom(source => source.SocialMediaPlatform != null ? source.SocialMediaPlatform.Icon : null));
        CreateMap<Domain.Models.Talk, Models.TalkViewModel>()
            .ForMember(destination => destination.BlueSkyHandle, options => options.Ignore());
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItemViewModel>()
            .ForMember(
                destination => destination.ItemTableName,
                options => options.MapFrom(source => source.ItemType.ToString()))
            .ForMember(
                destination => destination.ItemType,
                options => options.MapFrom(source => source.ItemType))
            .ForMember(destination => destination.Platform, options => options.Ignore());
        CreateMap<Domain.Models.MessageTemplate, Models.MessageTemplateViewModel>()
            .ForMember(destination => destination.Platform, options => options.Ignore());
        
        // RBAC Phase 1 mappings
        CreateMap<Domain.Models.ApplicationUser, Models.ApplicationUserViewModel>();
        
        // RBAC Phase 2 mappings
        CreateMap<Domain.Models.Role, Models.RoleViewModel>();

        // SocialMediaPlatform mappings (Issue #678)
        CreateMap<Domain.Models.SocialMediaPlatform, Models.SocialMediaPlatformViewModel>();
        CreateMap<Models.SocialMediaPlatformViewModel, Domain.Models.SocialMediaPlatform>();

    }
}