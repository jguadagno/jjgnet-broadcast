using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings: ISettings, IDatabaseSettings
{
    public string StorageAccount { get; set; }
    public string JJGNetDatabaseSqlServer { get; set; }
    public string ApiScopeUrl { get; set; }
    public string SwaggerClientId { get; set; }
    public string SwaggerClientSecret { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public required AutoMapperSettings AutoMapper { get; init; }
}