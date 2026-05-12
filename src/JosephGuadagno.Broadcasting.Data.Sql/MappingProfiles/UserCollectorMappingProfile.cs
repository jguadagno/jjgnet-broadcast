using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

/// <summary>
/// AutoMapper profile for user collector configuration entities
/// </summary>
public class UserCollectorMappingProfile : Profile
{
    public UserCollectorMappingProfile()
    {
        CreateMap<Models.UserCollectorFeedSource, Domain.Models.UserCollectorFeedSource>().ReverseMap();
        CreateMap<Models.UserCollectorYouTubeChannel, Domain.Models.UserCollectorYouTubeChannel>()
            .ForMember(dest => dest.ApiKey, opt => opt.Ignore())
            .ForMember(dest => dest.HasApiKey, opt => opt.Ignore())
            .ReverseMap();
        CreateMap<Models.UserCollectorSpeakingEngagement, Domain.Models.UserCollectorSpeakingEngagement>().ReverseMap();
    }
}
