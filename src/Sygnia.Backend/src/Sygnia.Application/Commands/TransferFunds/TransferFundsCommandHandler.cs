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
        var validationError = await ValidateAsync(request, cancellationToken);
        if (validationError is not null)
        {
            return Result<TransferResult>.Failure(validationError);
        }

        var accountsResult = await ResolveAccountsAsync(request, cancellationToken);
        if (accountsResult.IsFailure)
        {
            return Result<TransferResult>.Failure(accountsResult.Error);
        }

        var currencyError = ValidateCurrencies(request, accountsResult.Value.From, accountsResult.Value.To);
        if (currencyError is not null)
        {
            return Result<TransferResult>.Failure(currencyError);
        }

        var debit = BuildLeg(request, request.FromAccountId, "-DR", -request.Amount);
        var credit = BuildLeg(request, request.ToAccountId, "-CR", request.Amount);

        // Both legs land in one atomic transaction; idempotency on each leg's own
        // (AccountId, ExternalRef) is still enforced by the database, per leg.
        var result = await movementRepository.AddTransferAsync(debit, credit, cancellationToken);

        return result.IsSuccess
            ? Result<TransferResult>.Success(new TransferResult(result.Value.Debit, result.Value.Credit))
            : Result<TransferResult>.Failure(result.Error);
    }

    private async Task<Error?> ValidateAsync(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (validation.IsValid)
        {
            return null;
        }

        var message = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
        return new Error("transfer.invalid", message);
    }

    private async Task<Result<(Account From, Account To)>> ResolveAccountsAsync(
        TransferFundsCommand request, CancellationToken cancellationToken)
    {
        var fromAccount = await accountRepository.GetAsync(request.FromAccountId, cancellationToken);
        if (fromAccount is null)
        {
            return Result<(Account, Account)>.Failure(
                new Error("account.not_found", $"Account '{request.FromAccountId}' does not exist."));
        }

        var toAccount = await accountRepository.GetAsync(request.ToAccountId, cancellationToken);
        if (toAccount is null)
        {
            return Result<(Account, Account)>.Failure(
                new Error("account.not_found", $"Account '{request.ToAccountId}' does not exist."));
        }

        return Result<(Account, Account)>.Success((fromAccount, toAccount));
    }

    // Both legs share one Currency on the wire, but each account may have its own — reject a
    // transfer that doesn't match either side rather than let the balance SUM silently mix currencies.
    private static Error? ValidateCurrencies(TransferFundsCommand request, Account fromAccount, Account toAccount)
    {
        if (!string.Equals(fromAccount.Currency, request.Currency, StringComparison.Ordinal))
        {
            return new Error(
                "movement.currency.invalid",
                $"Account '{request.FromAccountId}' is '{fromAccount.Currency}'; transfer was submitted in '{request.Currency}'.");
        }

        if (!string.Equals(toAccount.Currency, request.Currency, StringComparison.Ordinal))
        {
            return new Error(
                "movement.currency.invalid",
                $"Account '{request.ToAccountId}' is '{toAccount.Currency}'; transfer was submitted in '{request.Currency}'.");
        }

        return null;
    }

    private static Movement BuildLeg(TransferFundsCommand request, string accountId, string refSuffix, decimal amount) =>
        new(
            accountId,
            $"{request.ExternalRef}{refSuffix}",
            request.Currency,
            amount,
            request.OccurredAt,
            request.Narration,
            request.RefNr,
            request.MovedBy,
            request.MovedDate);
}
