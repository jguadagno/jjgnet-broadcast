using AutoMapper;

namespace JosephGuadagno.Broadcasting.Web.MappingProfiles;

/// <summary>
/// Maps the models to the view models.
/// </summary>
public class WebMappingProfile: Profile
{
    /// <summary>
    /// The constructors which does the mapping.
    /// </summary>
    public WebMappingProfile()
    {
        CreateMap<Models.EngagementViewModel, Domain.Models.Engagement>();
        CreateMap<Models.TalkViewModel, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItemViewModel, Domain.Models.ScheduledItem>();

        CreateMap<Domain.Models.Engagement, Models.EngagementViewModel>();
        CreateMap<Domain.Models.Talk, Models.TalkViewModel>();
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItemViewModel>();
    }
}