namespace ProjectDefense.Core.Common;

public enum OperationError
{
    None,
    Validation,
    NotFound,
    Conflict,
    Forbidden
}

public sealed record OperationResult(bool Succeeded, OperationError Error, string? Message)
{
    public static OperationResult Success() => new(true, OperationError.None, null);

    public static OperationResult Fail(OperationError error, string message) => new(false, error, message);
}

public sealed record OperationResult<T>(bool Succeeded, OperationError Error, string? Message, T? Value)
{
    public static OperationResult<T> Success(T value) => new(true, OperationError.None, null, value);

    public static OperationResult<T> Fail(OperationError error, string message) => new(false, error, message, default);
}
