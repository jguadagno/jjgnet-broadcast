using Serilog.Events;
using Serilog.Sinks.AzureTableStorage;

namespace JosephGuadagno.Broadcasting.Serilog;

public class SerilogKeyGenerator : IKeyGenerator
{
    public string GeneratePartitionKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        return logEvent.Timestamp.UtcDateTime.ToString("yyyy-MM-dd");
    }

    public string GenerateRowKey(LogEvent logEvent, AzureTableStorageSinkOptions options)
    {
        return logEvent.Timestamp.UtcDateTime.TimeOfDay.ToString();
    }
}