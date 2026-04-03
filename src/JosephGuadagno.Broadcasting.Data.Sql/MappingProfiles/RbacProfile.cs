using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

/// <summary>
/// AutoMapper profile for RBAC entities
/// </summary>
public class RbacProfile : Profile
{
    public RbacProfile()
    {
        // ApplicationUser mappings
        CreateMap<Models.ApplicationUser, Domain.Models.ApplicationUser>()
            .ForMember(
                destination => destination.Roles,
                options => options.MapFrom(source => 
                    source.UserRoles.Select(ur => ur.Role).ToList()))
            .ReverseMap()
            .ForMember(
                destination => destination.UserRoles,
                options => options.Ignore())
            .ForMember(
                destination => destination.UserApprovalLogs,
                options => options.Ignore())
            .ForMember(
                destination => destination.AdminUserApprovalLogs,
                options => options.Ignore());

        // Role mappings
        CreateMap<Models.Role, Domain.Models.Role>().ReverseMap()
            .ForMember(
                destination => destination.UserRoles,
                options => options.Ignore());

        // UserRole mappings
        CreateMap<Models.UserRole, Domain.Models.UserRole>().ReverseMap();

        // UserApprovalLog mappings
        CreateMap<Models.UserApprovalLog, Domain.Models.UserApprovalLog>().ReverseMap()
            .ForMember(
                destination => destination.User,
                options => options.Ignore())
            .ForMember(
                destination => destination.AdminUser,
                options => options.Ignore());
    }
}
