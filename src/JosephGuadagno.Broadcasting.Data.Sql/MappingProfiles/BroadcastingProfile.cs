using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        // Sql to Domain
        CreateMap<Models.Engagement, Domain.Models.Engagement>();
        CreateMap<Models.Talk, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItem, Domain.Models.ScheduledItem>()
            .ForMember(destination => destination.ScheduleDateTime,
                options => options.MapFrom(source => source.SendOnDateTime));

        
        // Domain to Sql
        CreateMap<Domain.Models.Engagement, Models.Engagement>();
        CreateMap<Domain.Models.Talk, Models.Talk>()
            .ForMember(destination => destination.Engagement, options => options.Ignore());
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>()
            .ForMember(destination => destination.SendOnDateTime,
                options => options.MapFrom(source => source.ScheduleDateTime))
            .ForMember(destination => destination.MessageSentOn,
                options => options.MapFrom(source => source.ScheduleDateTime))
            .ForMember(destination => destination.MessageSent, options => options.Ignore())
            .ForMember(destination => destination.MessageSentOn, options => options.Ignore())
            .ForMember(destination => destination.Message, options => options.Ignore());

    }
}
/*
===================================================================================================
Talk -> Talk (Destination member list)
JosephGuadagno.Broadcasting.Domain.Models.Talk -> JosephGuadagno.Broadcasting.Data.Sql.Models.Talk (Destination member list)

Unmapped properties:
EngagementId
Engagement

*/