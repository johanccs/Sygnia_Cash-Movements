using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.SubmitMovement;

internal sealed class SubmitMovementCommandHandler(
    IMovementRepository movementRepository,
    IAccountRepository accountRepository) : IRequestHandler<SubmitMovementCommand, Result<Movement>>
{
    // Validation runs in ValidationBehaviour<,> before this handler is reached.
    public async Task<Result<Movement>> Handle(SubmitMovementCommand request, CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetAsync(request.AccountId, cancellationToken);
        if (account is null)
        {
            return Result<Movement>.Failure(
                new Error(ErrorCode.AccountNotFound, $"Account '{request.AccountId}' does not exist."));
        }

        // The balance SUM has no notion of currency, so a mismatched submission would silently
        // corrupt it rather than fail loudly — reject it here instead.
        var currencyError = account.EnsureCurrencyMatches(request.Currency);
        if (currencyError is not null)
        {
            return Result<Movement>.Failure(currencyError);
        }

        var movement = new Movement(
            request.AccountId,
            request.ExternalRef,
            request.Currency,
            request.Amount,
            request.OccurredAt,
            request.Narration,
            request.RefNr,
            request.MovedBy,
            request.MovedDate);

        // Idempotency lives in the database: AddAsync attempts the INSERT and turns a
        // 2627/2601 conflict into an OK-replay or ALREADY_EXISTS Result — never an exception.
        return await movementRepository.AddAsync(movement, cancellationToken);
    }
}
