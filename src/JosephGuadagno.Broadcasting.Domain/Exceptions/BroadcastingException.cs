namespace JosephGuadagno.Broadcasting.Domain.Exceptions;

public abstract class BroadcastingException : Exception
{
    public int? ApiErrorCode { get; }
    public string? ApiErrorMessage { get; }

    protected BroadcastingException(string message) : base(message) { }

    protected BroadcastingException(string message, Exception innerException)
        : base(message, innerException) { }

    protected BroadcastingException(string message, int? apiErrorCode, string? apiErrorMessage)
        : base(message)
    {
        ApiErrorCode = apiErrorCode;
        ApiErrorMessage = apiErrorMessage;
    }
}
