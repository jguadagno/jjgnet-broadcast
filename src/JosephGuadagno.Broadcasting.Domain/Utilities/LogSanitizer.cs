using System.Text.RegularExpressions;

namespace JosephGuadagno.Broadcasting.Domain.Utilities;

/// <summary>
/// Provides sanitization helpers for log messages to prevent log injection.
/// </summary>
public static class LogSanitizer
{
    private static readonly Regex ControlCharPattern = new(@"[\x00-\x1F\x7F]", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes a user-controlled string before it is written to a log entry.
    /// Strips all ASCII control characters (0x00–0x1F and 0x7F) to prevent log injection.
    /// </summary>
    /// <param name="value">The value to sanitize.</param>
    /// <returns>The sanitized string, or empty string if the value is null.</returns>
    public static string Sanitize(string? value) =>
        value is null ? string.Empty : ControlCharPattern.Replace(value, string.Empty);
}
