using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        // Sql models to Domain
        CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap();
        CreateMap<Models.Talk, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItem, Domain.Models.ScheduledItem>();
        CreateMap<Models.FeedCheck, Domain.Models.FeedCheck>().ReverseMap();
        CreateMap<Models.SyndicationFeedSource, Domain.Models.SyndicationFeedSource>().ReverseMap();
        CreateMap<Models.YouTubeSource, Domain.Models.YouTubeSource>().ReverseMap();
        CreateMap<Models.TokenRefresh, Domain.Models.TokenRefresh>().ReverseMap();
        
        // Domain to Sql models
        CreateMap<Domain.Models.Talk, Models.Talk>()
            .ForMember(destination => destination.Engagement, options => options.Ignore());
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>() ;
    }
}