using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.TransferFunds;

internal sealed class TransferFundsCommandHandler(
    IMovementRepository movementRepository,
    IValidator<TransferFundsCommand> validator) : IRequestHandler<TransferFundsCommand, Result<TransferResult>>
{
    public async Task<Result<TransferResult>> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            return Result<TransferResult>.Failure(new Error("transfer.invalid", message));
        }

        if (!await movementRepository.AccountExistsAsync(request.FromAccountId, cancellationToken))
        {
            return Result<TransferResult>.Failure(
                new Error("account.not_found", $"Account '{request.FromAccountId}' does not exist."));
        }

        if (!await movementRepository.AccountExistsAsync(request.ToAccountId, cancellationToken))
        {
            return Result<TransferResult>.Failure(
                new Error("account.not_found", $"Account '{request.ToAccountId}' does not exist."));
        }

        var debit = new Movement(
            request.FromAccountId,
            $"{request.ExternalRef}-DR",
            request.Currency,
            -request.Amount,
            request.OccurredAt,
            request.Narration,
            request.RefNr,
            request.MovedBy,
            request.MovedDate);

        var credit = new Movement(
            request.ToAccountId,
            $"{request.ExternalRef}-CR",
            request.Currency,
            request.Amount,
            request.OccurredAt,
            request.Narration,
            request.RefNr,
            request.MovedBy,
            request.MovedDate);

        // Both legs land in one atomic transaction; idempotency on each leg's own
        // (AccountId, ExternalRef) is still enforced by the database, per leg.
        var result = await movementRepository.AddTransferAsync(debit, credit, cancellationToken);

        return result.IsSuccess
            ? Result<TransferResult>.Success(new TransferResult(result.Value.Debit, result.Value.Credit))
            : Result<TransferResult>.Failure(result.Error);
    }
}
