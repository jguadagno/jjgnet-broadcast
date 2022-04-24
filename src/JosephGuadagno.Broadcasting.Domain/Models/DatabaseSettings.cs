using JosephGuadagno.Broadcasting.Domain.Interfaces;

namespace JosephGuadagno.Broadcasting.Domain.Models;

public class DatabaseSettings: IDatabaseSettings
{
    public string JJGNetDatabaseSqlServer { get; set; }
}