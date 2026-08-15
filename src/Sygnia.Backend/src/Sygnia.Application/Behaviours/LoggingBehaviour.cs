using MediatR;
using Microsoft.Extensions.Logging;

namespace Sygnia.Application.Behaviours;

/// <summary>
/// Logs entry, exit, and duration for every MediatR request. Registered once as an open
/// generic, so no handler needs to log this itself.
/// </summary>
internal sealed class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var startedAt = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            var response = await next();
            logger.LogInformation(
                "Handled {RequestName} in {ElapsedMs}ms",
                requestName,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{RequestName} failed after {ElapsedMs}ms",
                requestName,
                System.Diagnostics.Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
            throw;
        }
    }
}
