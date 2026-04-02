using AutoMapper;

namespace JosephGuadagno.Broadcasting.Data.Sql.MappingProfiles;

public class EmailTemplateProfile : Profile
{
    public EmailTemplateProfile()
    {
        CreateMap<Models.EmailTemplate, Domain.Models.EmailTemplate>().ReverseMap();
    }
}
