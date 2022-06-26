using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;

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
        
        // // Web to Domain
        // CreateMap<Models.EngagementViewModel, Domain.Models.Engagement>()
        //     .ForMember(destination => destination.StartDateTime, options => options.MapFrom(source =>
        //         new DateTimeOffset(source.StartDateTime.ToDateTimeUtc())))
        //     .ForMember(destination => destination.EndDateTime, options => options.MapFrom(source =>
        //         new DateTimeOffset(source.EndDateTime.ToDateTimeUtc())));
        // CreateMap<Models.TalkViewModel, Domain.Models.Talk>()
        //     .ForMember(destination => destination.StartDateTime, options => options.MapFrom(source =>
        //         new DateTimeOffset(source.StartDateTime, source.TalkTimezoneOffset)))
        //     .ForMember(destination => destination.EndDateTime, options => options.MapFrom(source =>
        //         new DateTimeOffset(source.EndDateTime, source.TalkTimezoneOffset)));
        // CreateMap<Models.ScheduledItemViewModel, Domain.Models.ScheduledItem>()
        //     .ForMember(destination => destination.ScheduleDateTime, options => options.MapFrom(source =>
        //         new DateTimeOffset(source.ScheduleDateTime, source.ScheduleOffset)));
        //     
        // // Domain to Web
        // CreateMap<Domain.Models.Engagement, Models.EngagementViewModel>()
        //     .ForMember(destination => destination.StartDateTime, options => options.MapFrom(source =>
        //         source.StartDateTime))
        //     .ForMember(destination => destination.StartDateTime,
        //         options => options.MapFrom(source => source.StartDateTime.DateTime))
        //     .ForMember(destination => destination.EndDateTime,
        //         options => options.MapFrom(source => source.EndDateTime.DateTime));
        // CreateMap<Domain.Models.Talk, Models.TalkViewModel>()
        //     .ForMember(destination => destination.TalkTimezoneOffset, options => options.MapFrom(source =>
        //         source.StartDateTime.Offset))
        //     .ForMember(destination => destination.StartDateTime,
        //         options => options.MapFrom(source => source.StartDateTime.DateTime))
        //     .ForMember(destination => destination.EndDateTime,
        //         options => options.MapFrom(source => source.EndDateTime.DateTime));
        // CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItemViewModel>()
        //     .ForMember(destination => destination.ScheduleOffset, options => options.MapFrom(source =>
        //         source.ScheduleDateTime.Offset))
        //     .ForMember(destination => destination.ScheduleDateTime,
        //         options => options.MapFrom(source => source.ScheduleDateTime.DateTime));
    }
}