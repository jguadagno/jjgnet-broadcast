using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        CreateMap<Models.Engagement, Domain.Models.Engagement>();
        CreateMap<Models.Talk, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItem, Domain.Models.ScheduledItem>()
            .ForMember(destination => destination.ScheduleDateTime,
                options => options.MapFrom(source => source.SendOnDateTime));

        

        CreateMap<Domain.Models.Engagement, Models.Engagement>();
        CreateMap<Domain.Models.Talk, Models.Talk>();
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>()
            .ForMember(destination => destination.MessageSentOn,
                options => options.MapFrom(source => source.ScheduleDateTime))
            .ForMember(destination => destination.MessageSent, options => options.Ignore())
            .ForMember(destination => destination.MessageSentOn, options => options.Ignore())
            .ForMember(destination => destination.Message, options => options.Ignore());

    }
}