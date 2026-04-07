namespace JosephGuadagno.Broadcasting.Domain.Exceptions;

/// <summary>
/// Thrown when an event cannot be published to Event Grid after all retry attempts are exhausted.
/// </summary>
public class EventPublishException : BroadcastingException
{
    public EventPublishException(string message) : base(message) { }

    public EventPublishException(string message, Exception innerException)
        : base(message, innerException) { }
}
