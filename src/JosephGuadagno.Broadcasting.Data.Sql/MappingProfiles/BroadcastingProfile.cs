using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        CreateMap<Data.Sql.Models.Engagement, Domain.Models.Engagement>();
        CreateMap<Data.Sql.Models.Talk, Domain.Models.Talk>();
        CreateMap<Data.Sql.Models.ScheduledItem, Domain.Models.ScheduledItem>();

        CreateMap<Domain.Models.Engagement, Models.Engagement>();
        CreateMap<Domain.Models.Talk, Models.Talk>();
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>();
    }
}