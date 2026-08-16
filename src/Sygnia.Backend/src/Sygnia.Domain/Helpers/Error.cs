namespace Sygnia.Domain;

/// <summary>
/// An expected business failure, carried as a value rather than thrown.
/// <para>
/// <see cref="Code"/> is stable and machine-readable — the transport layer maps it to a gRPC
/// status, so it must not be reworded casually. <see cref="Message"/> is for humans and logs.
/// </para>
/// </summary>
public sealed record Error
{
    public Error(string code, string message)
    {
        Guard.AgainstNullOrWhiteSpace(code, nameof(code));
        Guard.AgainstNullOrWhiteSpace(message, nameof(message));

        Code = code;
        Message = message;
    }

    /// <summary>Preferred overload — takes a known <see cref="ErrorCode"/> rather than a literal.</summary>
    public Error(ErrorCode code, string message)
        : this(code.ToCode(), message)
    {
    }

    public string Code { get; }

    public string Message { get; }
}
