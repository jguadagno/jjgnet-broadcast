using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings: ISettings
{
    public string StorageAccount { get; set; }
    public string ApiScopeUrl { get; set; }
    public string ScalarClientId { get; set; }
    public string JJGNetDatabaseSqlServer { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public required AutoMapperSettings AutoMapper { get; init; }
}