using Grpc.Core;
using Grpc.Core.Interceptors;

namespace Sygnia.Presentation;

/// <summary>
/// The one global error handler for this transport, per the root CLAUDE.md — the gRPC
/// equivalent of an HTTP <c>ProblemDetails</c> middleware. Catches anything a service method
/// didn't already turn into an <see cref="RpcException"/> via <see cref="ResultExtensions"/>,
/// logs the real exception, and rethrows a status-only <c>INTERNAL</c> so details never reach
/// the wire.
/// </summary>
internal sealed class ErrorInterceptor(ILogger<ErrorInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw; // already a deliberate, correctly-statused failure — pass through.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        try
        {
            await continuation(request, responseStream, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in {Method}", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
        }
    }
}
