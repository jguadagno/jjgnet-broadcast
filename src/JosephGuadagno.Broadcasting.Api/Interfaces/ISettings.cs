using JosephGuadagno.Broadcasting.Domain.Interfaces;
using JosephGuadagno.Broadcasting.Domain.Models;

namespace JosephGuadagno.Broadcasting.Api.Interfaces;

public interface ISettings
{
    public string StorageAccount { get; set; }
    public string ApiScopeUrl { get; set; }
    public string ScalarClientId { get; set; }

    public string JJGNetDatabaseSqlServer { get; set; }

    /// <summary>
    /// The AutoMapper settings.
    /// </summary>
    public AutoMapperSettings AutoMapper { get; init; }
}