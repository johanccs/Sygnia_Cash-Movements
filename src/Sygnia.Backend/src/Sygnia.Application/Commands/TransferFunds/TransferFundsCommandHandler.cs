using FluentValidation;
using MediatR;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Commands.TransferFunds;

internal sealed class TransferFundsCommandHandler(
    IMovementRepository movementRepository,
    IAccountRepository accountRepository,
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

        var fromAccount = await accountRepository.GetAsync(request.FromAccountId, cancellationToken);
        if (fromAccount is null)
        {
            return Result<TransferResult>.Failure(
                new Error("account.not_found", $"Account '{request.FromAccountId}' does not exist."));
        }

        var toAccount = await accountRepository.GetAsync(request.ToAccountId, cancellationToken);
        if (toAccount is null)
        {
            return Result<TransferResult>.Failure(
                new Error("account.not_found", $"Account '{request.ToAccountId}' does not exist."));
        }

        // Both legs share one Currency on the wire, but each account may have its own —
        // reject a transfer that doesn't match either side rather than let the balance SUM
        // silently mix currencies.
        if (!string.Equals(fromAccount.Currency, request.Currency, StringComparison.Ordinal))
        {
            return Result<TransferResult>.Failure(new Error(
                "movement.currency.invalid",
                $"Account '{request.FromAccountId}' is '{fromAccount.Currency}'; transfer was submitted in '{request.Currency}'."));
        }

        if (!string.Equals(toAccount.Currency, request.Currency, StringComparison.Ordinal))
        {
            return Result<TransferResult>.Failure(new Error(
                "movement.currency.invalid",
                $"Account '{request.ToAccountId}' is '{toAccount.Currency}'; transfer was submitted in '{request.Currency}'."));
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
