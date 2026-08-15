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
    public static RpcException ToRpcException(this Error error)
    {
        var status = error.Code switch
        {
            "account.not_found" => StatusCode.NotFound,
            "movement.already_exists" => StatusCode.AlreadyExists,
            _ when error.Code.EndsWith(".invalid", StringComparison.Ordinal) => StatusCode.InvalidArgument,
            _ => StatusCode.Internal,
        };

        return new RpcException(new Status(status, error.Message));
    }
}
