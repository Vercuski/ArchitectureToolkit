namespace ArchitectureToolkit.Application.Abstractions;

public enum ResultErrorType { NotFound, Validation, Conflict }
public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultErrorType ErrorType { get; }

    private Result(bool isSuccess, T? value, string? error, ResultErrorType errorType)
    {
        (IsSuccess, Value, Error, ErrorType) = (isSuccess, value, error, errorType);
    }

    public static Result<T> Success(T value)
    {
        return new(true, value, null, default);
    }

    public static Result<T> Failure(string error, ResultErrorType errorType)
    {
        return new(false, default, error, errorType);
    }
}
