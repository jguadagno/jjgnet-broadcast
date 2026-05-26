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

        CreateMap<Models.UserRandomPostSettings, Domain.Models.UserRandomPostSettings>()
            .ForMember(destination => destination.ExcludedCategories, options => options.MapFrom(source => SplitCsv(source.ExcludedCategories)))
            .ReverseMap()
            .ForMember(destination => destination.ExcludedCategories, options => options.MapFrom(source => JoinCsv(source.ExcludedCategories)));

        CreateMap<Models.UserEventPublisherMapping, Domain.Models.UserEventPublisherMapping>().ReverseMap();
    }

    private static List<string> SplitCsv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string JoinCsv(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return string.Empty;
        }

        return string.Join(",", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim()));
    }
}
