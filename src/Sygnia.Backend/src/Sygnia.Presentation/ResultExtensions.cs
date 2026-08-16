using Grpc.Core;
using Sygnia.Domain;

namespace Sygnia.Presentation;

/// <summary>
/// The one place <see cref="Error.Code"/> gets mapped to a gRPC <see cref="StatusCode"/>, per
/// the root CLAUDE.md: validation -&gt; INVALID_ARGUMENT; unknown account -&gt; NOT_FOUND; same
/// key/different fields -&gt; ALREADY_EXISTS naming the conflict; anything unexpected -&gt;
/// INTERNAL. Every gRPC method calls this on a failed <see cref="Result{T}"/> rather than
/// deciding a status itself.
/// </summary>
internal static class ResultExtensions
{
    public static RpcException ToRpcException(this Error error) =>
        new(new Status(ToStatusCode(error.Code), error.Message));

    /// <summary>
    /// Matches on the known <see cref="ErrorCode"/> string first — the enum enumerates every
    /// code the codebase actually raises, and <c>Sygnia.Tests</c> pins each one to its expected
    /// status, so an unmapped new code fails that test rather than silently falling through.
    /// The suffix-based fallback only covers a code this switch does not yet know about.
    /// </summary>
    private static StatusCode ToStatusCode(string code) => code switch
    {
        var c when c == ErrorCode.AccountNotFound.ToCode() || c == ErrorCode.UserNotFound.ToCode() => StatusCode.NotFound,
        var c when c == ErrorCode.AccountAlreadyExists.ToCode()
            || c == ErrorCode.MovementAlreadyExists.ToCode()
            || c == ErrorCode.UserAlreadyExists.ToCode() => StatusCode.AlreadyExists,
        var c when c == ErrorCode.AccountInvalid.ToCode()
            || c == ErrorCode.AccountNameInvalid.ToCode()
            || c == ErrorCode.AccountContactPersonInvalid.ToCode()
            || c == ErrorCode.MovementInvalid.ToCode()
            || c == ErrorCode.MovementCurrencyInvalid.ToCode()
            || c == ErrorCode.TransferInvalid.ToCode()
            || c == ErrorCode.BalanceInvalid.ToCode()
            || c == ErrorCode.StatementInvalid.ToCode()
            || c == ErrorCode.UserInvalid.ToCode() => StatusCode.InvalidArgument,
        _ when code.EndsWith(".invalid", StringComparison.Ordinal) => StatusCode.InvalidArgument,
        _ => StatusCode.Internal,
    };
}
