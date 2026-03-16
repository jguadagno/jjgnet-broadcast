using JosephGuadagno.Broadcasting.Domain.Exceptions;

namespace JosephGuadagno.Broadcasting.Managers.Twitter.Exceptions;

public class TwitterPostException : BroadcastingException
{
    public TwitterPostException(string message) : base(message) { }

    public TwitterPostException(string message, Exception innerException)
        : base(message, innerException) { }

    public TwitterPostException(string message, int? apiErrorCode, string? apiErrorMessage)
        : base(message, apiErrorCode, apiErrorMessage) { }
}
