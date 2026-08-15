using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.SubmitMovement;

internal sealed class SubmitMovementCommandHandler(
    IMovementRepository movementRepository,
    IValidator<SubmitMovementCommand> validator) : IRequestHandler<SubmitMovementCommand, Result<Movement>>
{
    public async Task<Result<Movement>> Handle(SubmitMovementCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<Movement>.Failure(ToValidationError(validation));
        }

        if (!await movementRepository.AccountExistsAsync(request.AccountId, cancellationToken))
        {
            return Result<Movement>.Failure(
                new Error("account.not_found", $"Account '{request.AccountId}' does not exist."));
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

    private static Error ToValidationError(FluentValidation.Results.ValidationResult validation)
    {
        var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return new Error("movement.invalid", message);
    }
}
