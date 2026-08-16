using System.Reflection;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Sygnia.Domain;

namespace Sygnia.Application.Behaviours;

/// <summary>
/// Runs every registered <c>IValidator&lt;TRequest&gt;</c> once, before the handler, so each
/// command handler no longer repeats its own validate-then-map-to-<see cref="Error"/>
/// boilerplate. Only requests implementing <see cref="IValidatedRequest"/> are short-circuited
/// on failure — everything else passes through untouched, so registering this open generic is
/// safe even for requests (like the streaming statement query) that don't return a
/// <c>Result&lt;T&gt;</c> at all. A request that implements <see cref="IValidatedRequest"/> but
/// has no validator registered is a wiring bug, not a pass-through case, so it throws rather
/// than silently skipping validation.
/// </summary>
internal sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IValidatedRequest validated)
        {
            return await next();
        }

        if (!validators.Any())
        {
            throw new InvalidOperationException(
                $"{typeof(TRequest).Name} implements {nameof(IValidatedRequest)} but has no " +
                $"IValidator<{typeof(TRequest).Name}> registered.");
        }

        var failures = await CollectFailuresAsync(request, cancellationToken);
        return failures.Count == 0
            ? await next()
            : BuildFailureResponse(failures, validated.ValidationErrorCode);
    }

    private async Task<List<ValidationFailure>> CollectFailuresAsync(
        TRequest request, CancellationToken cancellationToken)
    {
        var results = await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken)));
        return results.SelectMany(r => r.Errors).ToList();
    }

    private static TResponse BuildFailureResponse(List<ValidationFailure> failures, ErrorCode code)
    {
        var message = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = new Error(code, message);

        // TResponse is always a closed Result<T> for any request implementing IValidatedRequest
        // — invoking its static Failure(Error) factory by reflection avoids the pipeline needing
        // to know T for every command's response type.
        var failureMethod = typeof(TResponse).GetMethod(nameof(Result<object>.Failure), BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{typeof(TResponse)} has no static Failure(Error) factory.");
        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
