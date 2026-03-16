using JosephGuadagno.Broadcasting.Domain.Exceptions;

namespace JosephGuadagno.Broadcasting.Managers.Bluesky.Exceptions;

public class BlueskyPostException : BroadcastingException
{
    public BlueskyPostException(string message) : base(message) { }

    public BlueskyPostException(string message, Exception innerException)
        : base(message, innerException) { }

    public BlueskyPostException(string message, int? apiErrorCode, string? apiErrorMessage)
        : base(message, apiErrorCode, apiErrorMessage) { }
}
