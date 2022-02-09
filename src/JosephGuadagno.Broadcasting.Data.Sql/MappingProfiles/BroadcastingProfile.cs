using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        CreateMap<Models.Engagement, Domain.Models.Engagement>();
        CreateMap<Models.Talk, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItem, Domain.Models.ScheduledItem>();

        CreateMap<Domain.Models.Engagement, Models.Engagement>();
        CreateMap<Domain.Models.Talk, Models.Talk>();
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>();
    }
}