using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

/// <summary>
/// AutoMapper profile for UserOAuthToken entities
/// </summary>
public class UserOAuthTokenMappingProfile : Profile
{
    public UserOAuthTokenMappingProfile()
    {
        CreateMap<Models.UserOAuthToken, Domain.Models.UserOAuthToken>().ReverseMap()
            .ForMember(
                destination => destination.SocialMediaPlatform,
                options => options.Ignore());
    }
}
