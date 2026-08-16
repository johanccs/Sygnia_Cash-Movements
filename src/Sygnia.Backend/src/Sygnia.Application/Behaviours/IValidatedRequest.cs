using Sygnia.Domain;

namespace Sygnia.Application.Behaviours;

/// <summary>
/// Opts a command into <see cref="ValidationBehaviour{TRequest,TResponse}"/>, naming the
/// <see cref="ErrorCode"/> the pipeline should report when its <c>IValidator&lt;TRequest&gt;</c>
/// fails. Requests that do not implement this pass through the behaviour unchanged, so it is
/// safe to register globally without affecting handlers that validate themselves.
/// </summary>
internal interface IValidatedRequest
{
    ErrorCode ValidationErrorCode { get; }
}
