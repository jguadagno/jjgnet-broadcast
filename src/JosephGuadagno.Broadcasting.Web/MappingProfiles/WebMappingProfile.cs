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
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore());
        CreateMap<Models.TalkViewModel, Domain.Models.Talk>()
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore());
        CreateMap<Models.ScheduledItemViewModel, Domain.Models.ScheduledItem>()
            .ForMember(
                destination => destination.ItemType,
                options => options.MapFrom(source => Enum.Parse<ScheduledItemType>(source.ItemTableName)))
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore());
        CreateMap<Models.MessageTemplateViewModel, Domain.Models.MessageTemplate>()
            .ForMember(destination => destination.CreatedByEntraOid, options => options.Ignore());

        CreateMap<Domain.Models.Engagement, Models.EngagementViewModel>()
            .ForMember(destination => destination.TimeZones, options => options.Ignore());
        CreateMap<Domain.Models.Talk, Models.TalkViewModel>();
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItemViewModel>()
            .ForMember(
                destination => destination.ItemTableName,
                options => options.MapFrom(source => source.ItemType.ToString()));
        CreateMap<Domain.Models.MessageTemplate, Models.MessageTemplateViewModel>();
        
        // RBAC Phase 1 mappings
        CreateMap<Domain.Models.ApplicationUser, Models.ApplicationUserViewModel>();
    }
}