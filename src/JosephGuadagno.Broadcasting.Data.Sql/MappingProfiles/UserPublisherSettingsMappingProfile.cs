using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

/// <summary>
/// AutoMapper profile for per-publisher settings entity mappings
/// </summary>
public class UserPublisherSettingsMappingProfile : Profile
{
    public UserPublisherSettingsMappingProfile()
    {
        CreateMap<Models.UserPublisherBlueskySettings, Domain.Models.UserPublisherBlueskySettings>().ReverseMap();
        CreateMap<Models.UserPublisherTwitterSettings, Domain.Models.UserPublisherTwitterSettings>().ReverseMap();
        CreateMap<Models.UserPublisherLinkedInSettings, Domain.Models.UserPublisherLinkedInSettings>().ReverseMap();
        CreateMap<Models.UserPublisherFacebookSettings, Domain.Models.UserPublisherFacebookSettings>().ReverseMap();
    }
}
