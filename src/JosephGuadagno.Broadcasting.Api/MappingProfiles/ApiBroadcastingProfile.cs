using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Api.MappingProfiles;

public class ApiBroadcastingProfile : Profile
{
    public ApiBroadcastingProfile()
    {
        // Domain → Response DTOs
        CreateMap<YouTubeItem, YouTubeItemResponse>();
        CreateMap<Engagement, EngagementResponse>();
        CreateMap<Talk, TalkResponse>();
        CreateMap<ScheduledItem, ScheduledItemResponse>();
        CreateMap<MessageTemplate, MessageTemplateResponse>();
        CreateMap<SocialMediaPlatform, SocialMediaPlatformResponse>();
        CreateMap<EngagementSocialMediaPlatform, EngagementSocialMediaPlatformResponse>();
        CreateMap<UserPublisherBlueskySettings, BlueskySettingsResponse>();
        CreateMap<UserPublisherTwitterSettings, TwitterSettingsResponse>();
        CreateMap<UserPublisherLinkedInSettings, LinkedInSettingsResponse>();
        CreateMap<UserPublisherFacebookSettings, FacebookSettingsResponse>();
        CreateMap<UserRandomPostSettings, UserRandomPostSettingsResponse>();
        CreateMap<UserEventPublisherMapping, UserEventPublisherMappingResponse>();

        // Request DTOs → Domain
        CreateMap<YouTubeItemRequest, YouTubeItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AddedOn, o => o.Ignore())
            .ForMember(d => d.ItemLastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags ?? new List<string>()));

        CreateMap<EngagementRequest, Engagement>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Talks, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());

        CreateMap<TalkRequest, Talk>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.EngagementId, o => o.Ignore());

        CreateMap<ScheduledItemRequest, ScheduledItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.MessageSent, o => o.Ignore())
            .ForMember(d => d.MessageSentOn, o => o.Ignore())
            .ForMember(d => d.SourceItemDisplayName, o => o.Ignore());

        CreateMap<MessageTemplateRequest, MessageTemplate>()
            .ForMember(d => d.SocialMediaPlatformId, o => o.Ignore())
            .ForMember(d => d.MessageType, o => o.Ignore());

        CreateMap<SocialMediaPlatformRequest, SocialMediaPlatform>()
            .ForMember(d => d.Id, o => o.Ignore());

        CreateMap<EngagementSocialMediaPlatformRequest, EngagementSocialMediaPlatform>()
            .ForMember(d => d.EngagementId, o => o.Ignore())
            .ForMember(d => d.Engagement, o => o.Ignore())
            .ForMember(d => d.SocialMediaPlatform, o => o.Ignore());

        // Syndication Feed Source
        CreateMap<SyndicationFeedItem, SyndicationFeedItemResponse>();
        CreateMap<SyndicationFeedItemRequest, SyndicationFeedItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.AddedOn, o => o.Ignore())
            .ForMember(d => d.ItemLastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.Tags, o => o.MapFrom(s => s.Tags ?? new List<string>()));

        // User Collector Feed Source
        CreateMap<UserCollectorFeedSource, UserCollectorFeedSourceResponse>();
        CreateMap<UserCollectorFeedSourceRequest, UserCollectorFeedSource>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());

        // User Collector YouTube Channel
        CreateMap<UserCollectorYouTubeChannel, UserCollectorYouTubeChannelResponse>();
        CreateMap<CreateUserCollectorYouTubeChannelRequest, UserCollectorYouTubeChannel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.HasApiKey, o => o.Ignore());
        CreateMap<UpdateUserCollectorYouTubeChannelRequest, UserCollectorYouTubeChannel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.HasApiKey, o => o.Ignore());

        // User Collector Speaking Engagement
        CreateMap<UserCollectorSpeakingEngagementRequest, UserCollectorSpeakingEngagement>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());
        CreateMap<UserCollectorSpeakingEngagement, UserCollectorSpeakingEngagementResponse>();

        // User Random Post Settings
        CreateMap<CreateUserRandomPostSettingsRequest, UserRandomPostSettings>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForMember(d => d.ExcludedCategories, o => o.MapFrom(s => s.ExcludedCategories ?? new List<string>()));
        CreateMap<UpdateUserRandomPostSettingsRequest, UserRandomPostSettings>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, sourceMember) => sourceMember is not null));

        // User Event Publisher Mapping
        CreateMap<CreateUserEventPublisherMappingRequest, UserEventPublisherMapping>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());
        CreateMap<UpdateUserEventPublisherMappingRequest, UserEventPublisherMapping>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore())
            .ForAllMembers(o => o.Condition((_, _, sourceMember) => sourceMember is not null));
    }
}
