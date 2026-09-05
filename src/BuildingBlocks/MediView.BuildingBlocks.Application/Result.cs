namespace MediView.BuildingBlocks.Application;

// Error
public sealed record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

// Result
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException();
        IsSuccess = isSuccess; Error = error;
    }
    
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

// Result<T>
public class Result<T>: Result
{
    private readonly T? _value;
    protected internal Result(T? value, bool isSuccess, Error error): base(isSuccess, error) => _value = value;
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("No value on a failure.");
    public static implicit operator Result<T>(T value) => Success(value);
}