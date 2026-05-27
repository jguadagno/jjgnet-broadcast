using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

/// <summary>
/// AutoMapper profile for per-publisher settings entity mappings
/// </summary>
public class UserPlatformSettingsMappingProfile : Profile
{
    public UserPlatformSettingsMappingProfile()
    {
        CreateMap<Models.UserPlatformBlueskySettings, Domain.Models.UserPlatformBlueskySettings>().ReverseMap();
        CreateMap<Models.UserPlatformTwitterSettings, Domain.Models.UserPlatformTwitterSettings>().ReverseMap();
        CreateMap<Models.UserPlatformLinkedInSettings, Domain.Models.UserPlatformLinkedInSettings>().ReverseMap();
        CreateMap<Models.UserPlatformFacebookSettings, Domain.Models.UserPlatformFacebookSettings>().ReverseMap();

        CreateMap<Models.UserRandomPostSettings, Domain.Models.UserRandomPostSettings>()
            .ForMember(destination => destination.ExcludedCategories, options => options.MapFrom(source => SplitCsv(source.ExcludedCategories)))
            .ReverseMap()
            .ForMember(destination => destination.ExcludedCategories, options => options.MapFrom(source => JoinCsv(source.ExcludedCategories)));

        CreateMap<Models.UserEventDispatcherMapping, Domain.Models.UserEventDispatcherMapping>().ReverseMap();
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

