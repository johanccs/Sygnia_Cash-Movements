namespace Sygnia.Domain;

/// <summary>
/// The outcome of an operation that is expected to fail sometimes.
/// <para>
/// Constructors throw and methods return <see cref="Result{T}"/> — these are not in conflict.
/// A broken invariant (null account id) is an object that must never exist, so the constructor
/// refuses. An expected outcome (name too long, duplicate reference) is a value the caller has
/// to handle, so it comes back here and the compiler makes them look at it.
/// </para>
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(T? value, Error? error, bool isSuccess)
    {
        _value = value;
        _error = error;
        IsSuccess = isSuccess;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The value, when successful. Reading it on a failure is a bug in the caller, not an
    /// expected outcome, so it throws rather than returning null.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Cannot read Value: the result is a failure ({_error!.Code}). Check IsSuccess first.");

    /// <summary>The error, when failed. Reading it on a success is likewise a caller bug.</summary>
    public Error Error => IsFailure
        ? _error!
        : throw new InvalidOperationException(
            "Cannot read Error: the result is a success. Check IsFailure first.");

    public static Result<T> Success(T value) => new(value, null, true);

    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(default, error, false);
    }
}
