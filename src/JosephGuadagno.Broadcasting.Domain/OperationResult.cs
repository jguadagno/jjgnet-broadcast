namespace JosephGuadagno.Broadcasting.Domain;

/// <summary>
/// Represents the result of an operation that returns a value.
/// </summary>
public class OperationResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Value { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }

    public static OperationResult<T> Success(T value) =>
        new() { IsSuccess = true, Value = value };

    public static OperationResult<T> Failure(string error, Exception? ex = null) =>
        new() { IsSuccess = false, ErrorMessage = error, Exception = ex };
}

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
public class OperationResult
{
    public bool IsSuccess { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }

    public static OperationResult Success() =>
        new() { IsSuccess = true };

    public static OperationResult Failure(string error, Exception? ex = null) =>
        new() { IsSuccess = false, ErrorMessage = error, Exception = ex };
}
