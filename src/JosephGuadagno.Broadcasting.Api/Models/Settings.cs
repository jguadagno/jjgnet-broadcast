using JosephGuadagno.Broadcasting.Domain.Interfaces;
using ISettings = JosephGuadagno.Broadcasting.Api.Interfaces.ISettings;

namespace JosephGuadagno.Broadcasting.Api.Models;

public class Settings: ISettings, IDatabaseSettings
{
    public string AppInsightsKey { get; set; }
    public string StorageAccount { get; set; }
    public string JJGNetDatabaseSqlServer { get; set; }
}