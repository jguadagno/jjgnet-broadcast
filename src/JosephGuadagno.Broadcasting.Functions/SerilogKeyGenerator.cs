using JosephGuadagno.Extensions.Types;
using Serilog.Events;
using Serilog.Sinks.AzureTableStorage.KeyGenerator;

namespace JosephGuadagno.Broadcasting.Functions;

public class SerilogKeyGenerator: IKeyGenerator
{
    public string GeneratePartitionKey(LogEvent logEvent)
    {
        return logEvent.Timestamp.UtcDateTime.ToString("yyyy-MM-dd");
    }

    public string GenerateRowKey(LogEvent logEvent, string suffix = null)
    {
        return logEvent.Timestamp.UtcDateTime.TimeOfDay.ToString();
    }
}