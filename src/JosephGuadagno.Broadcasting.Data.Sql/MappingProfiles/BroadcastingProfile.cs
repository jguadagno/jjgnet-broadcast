using AutoMapper;
using JosephGuadagno.Broadcasting.Domain.Enums;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class BroadcastingProfile: Profile
{
    public BroadcastingProfile()
    {
        // Sql models to Domain
        CreateMap<Models.Engagement, Domain.Models.Engagement>().ReverseMap();
        CreateMap<Models.Talk, Domain.Models.Talk>();
        CreateMap<Models.ScheduledItem, Domain.Models.ScheduledItem>()
            .ForMember(
                destination => destination.ItemType,
                options => options.MapFrom(source => Enum.Parse<ScheduledItemType>(source.ItemTableName)))
            .ForMember(
                destination => destination.SourceItemDisplayName,
                options => options.Ignore());
        CreateMap<Models.FeedCheck, Domain.Models.FeedCheck>().ReverseMap();
        CreateMap<Models.MessageTemplate, Domain.Models.MessageTemplate>()
            .ForMember(dest => dest.Platform,
                opt => opt.MapFrom(src => src.SocialMediaPlatform != null ? src.SocialMediaPlatform.Name : string.Empty))
            .ReverseMap()
            .ForMember(dest => dest.SocialMediaPlatform, opt => opt.Ignore());
        CreateMap<Models.TokenRefresh, Domain.Models.TokenRefresh>().ReverseMap();

        CreateMap<Models.SyndicationFeedSource, Domain.Models.SyndicationFeedSource>()
            .ForMember(
                destination => destination.Tags,
                options => options.MapFrom(source => source.SourceTags.Select(st => st.Tag).ToList()))
            .ReverseMap()
            .ForMember(
                destination => destination.Tags,
                options => options.MapFrom(source => source.Tags.Count > 0 ? string.Join(",", source.Tags) : null))
            .ForMember(
                destination => destination.SourceTags,
                options => options.Ignore());

        CreateMap<Models.YouTubeSource, Domain.Models.YouTubeSource>()
            .ForMember(
                destination => destination.Tags,
                options => options.MapFrom(source => source.SourceTags.Select(st => st.Tag).ToList()))
            .ReverseMap()
            .ForMember(
                destination => destination.Tags,
                options => options.MapFrom(source => source.Tags.Count > 0 ? string.Join(",", source.Tags) : null))
            .ForMember(
                destination => destination.SourceTags,
                options => options.Ignore());

        CreateMap<Models.SocialMediaPlatform, Domain.Models.SocialMediaPlatform>().ReverseMap();
        CreateMap<Models.EngagementSocialMediaPlatform, Domain.Models.EngagementSocialMediaPlatform>().ReverseMap();

        // Domain to Sql models
        CreateMap<Domain.Models.Talk, Models.Talk>()
            .ForMember(destination => destination.Engagement, options => options.Ignore());
        CreateMap<Domain.Models.ScheduledItem, Models.ScheduledItem>()
            .ForMember(
                destination => destination.ItemTableName,
                options => options.MapFrom(source => source.ItemType.ToString()))
            .ForMember(
                destination => destination.SocialMediaPlatform,
                options => options.Ignore());
    }
}