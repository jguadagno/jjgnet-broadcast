using AutoMapper;
using JosephGuadagno.Broadcasting.Api.Dtos;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Api.MappingProfiles;

public class ApiBroadcastingProfile : Profile
{
    public ApiBroadcastingProfile()
    {
        // Domain → Response DTOs
        CreateMap<Engagement, EngagementResponse>();
        CreateMap<Talk, TalkResponse>();
        CreateMap<ScheduledItem, ScheduledItemResponse>();
        CreateMap<MessageTemplate, MessageTemplateResponse>();

        // Request DTOs → Domain
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
            .ForMember(d => d.MessageSentOn, o => o.Ignore());

        CreateMap<MessageTemplateRequest, MessageTemplate>()
            .ForMember(d => d.Platform, o => o.Ignore())
            .ForMember(d => d.MessageType, o => o.Ignore());
    }
}
