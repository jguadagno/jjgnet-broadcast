using Microsoft.Extensions.Logging;

namespace JosephGuadagno.Broadcasting.Domain;

public static class ILoggerExtension
{
    public static void LogCustomEvent(this ILogger logger, string eventName, Dictionary<string, string>? properties = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(eventName);

        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        string message = "{microsoft.custom_event.name} ";
        object[] values;
        if (properties is not null && properties.Count > 0)
        {
            foreach (var property in properties)
            {
                message += "{" + property.Key + "} ";
            }
            values = new[] { eventName }.Concat(properties.Values).ToArray<object>();
        }
        else
        {
            values = [eventName];
        }
        logger.LogInformation(message, values);
    }
}