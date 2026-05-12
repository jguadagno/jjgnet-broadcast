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
        CreateMap<UserPublisherSetting, UserPublisherSettingResponse>();
        CreateMap<BlueskyPublisherSetting, BlueskyPublisherSettingResponse>();
        CreateMap<TwitterPublisherSetting, TwitterPublisherSettingResponse>();
        CreateMap<FacebookPublisherSetting, FacebookPublisherSettingResponse>();
        CreateMap<LinkedInPublisherSetting, LinkedInPublisherSettingResponse>();

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

        CreateMap<UserPublisherSettingRequest, UserPublisherSettingUpdate>()
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.SocialMediaPlatformId, o => o.Ignore());

        CreateMap<BlueskyPublisherSettingRequest, BlueskyPublisherSettingUpdate>();
        CreateMap<TwitterPublisherSettingRequest, TwitterPublisherSettingUpdate>();
        CreateMap<FacebookPublisherSettingRequest, FacebookPublisherSettingUpdate>();
        CreateMap<LinkedInPublisherSettingRequest, LinkedInPublisherSettingUpdate>();

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
        CreateMap<UserCollectorYouTubeChannelRequest, UserCollectorYouTubeChannel>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());

        // User Collector Speaking Engagement
        CreateMap<UserCollectorSpeakingEngagementRequest, UserCollectorSpeakingEngagement>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());
        CreateMap<UserCollectorSpeakingEngagement, UserCollectorSpeakingEngagementResponse>();

        // User Collector Scheduled Item
        CreateMap<UserCollectorScheduledItemRequest, UserCollectorScheduledItem>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedByEntraOid, o => o.Ignore())
            .ForMember(d => d.CreatedOn, o => o.Ignore())
            .ForMember(d => d.LastUpdatedOn, o => o.Ignore());
        CreateMap<UserCollectorScheduledItem, UserCollectorScheduledItemResponse>();
    }
}
