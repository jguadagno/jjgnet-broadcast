using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Events;
using Serilog.Exceptions;

namespace JosephGuadagno.Broadcasting.Serilog;

public static class LoggingExtensions
{
    public static LoggerConfiguration ConfigureSerilog(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        string applicationName,
        string logFilePath)
    {
        var loggingStorageAccount = configuration["Settings:LoggingStorageAccount"];
        
        return loggerConfiguration
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Azure", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithAssemblyName()
            .Enrich.WithAssemblyVersion(true)
            .Enrich.WithExceptionDetails()
            .Enrich.WithProperty("Application", applicationName)
            .Destructure.ToMaximumDepth(4)
            .Destructure.ToMaximumStringLength(100)
            .Destructure.ToMaximumCollectionCount(10)
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
            .WriteTo.AzureTableStorage(loggingStorageAccount!, storageTableName: "Logging", keyGenerator: new SerilogKeyGenerator())
            .WriteTo.OpenTelemetry();
    }
}
