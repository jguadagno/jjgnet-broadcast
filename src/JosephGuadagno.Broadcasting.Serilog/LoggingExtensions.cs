using Serilog;
// ReSharper disable once RedundantUsingDirective
using Serilog.Events; // This is needed for non-debug builds
using Serilog.Exceptions;

namespace JosephGuadagno.Broadcasting.Serilog;

public static class LoggingExtensions
{
    public static LoggerConfiguration ConfigureSerilog(
        this LoggerConfiguration loggerConfiguration,
        string applicationName,
        string logFilePath)
    {

        return loggerConfiguration
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Azure", LogEventLevel.Warning)
#endif
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
            .WriteTo.AzureTableStorage("TableAccount", storageTableName: "Logging", keyGenerator: new SerilogKeyGenerator())
            .WriteTo.OpenTelemetry();
    }
}