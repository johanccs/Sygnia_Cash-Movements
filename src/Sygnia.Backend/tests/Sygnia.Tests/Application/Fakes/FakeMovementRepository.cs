using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Tests.Application.Fakes;

/// <summary>
/// In-memory stand-in for the database's own idempotency check, so handler tests can exercise
/// OK / replay / ALREADY_EXISTS without a real database.
/// </summary>
public sealed class FakeMovementRepository : IMovementRepository
{
    private readonly Dictionary<(string AccountId, string ExternalRef), Movement> _stored = new();
    public HashSet<string> ExistingAccountIds { get; } = new();

    public Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingAccountIds.Contains(accountId));

    public Task<Result<Movement>> AddAsync(Movement movement, CancellationToken cancellationToken)
    {
        var key = (movement.AccountId, movement.ExternalRef);
        if (_stored.TryGetValue(key, out var existing))
        {
            var same = existing.Amount == movement.Amount
                && existing.Currency == movement.Currency
                && existing.OccurredAt == movement.OccurredAt;

            return Task.FromResult(same
                ? Result<Movement>.Success(existing)
                : Result<Movement>.Failure(new Error(
                    "movement.already_exists",
                    $"'{movement.ExternalRef}' already exists for '{movement.AccountId}' with different amount/currency/occurredAt.")));
        }

        _stored[key] = movement;
        return Task.FromResult(Result<Movement>.Success(movement));
    }

    public async Task<Result<(Movement Debit, Movement Credit)>> AddTransferAsync(
        Movement debit,
        Movement credit,
        CancellationToken cancellationToken)
    {
        var debitResult = await AddAsync(debit, cancellationToken);
        if (debitResult.IsFailure)
        {
            return Result<(Movement, Movement)>.Failure(debitResult.Error);
        }

        var creditResult = await AddAsync(credit, cancellationToken);
        if (creditResult.IsFailure)
        {
            return Result<(Movement, Movement)>.Failure(creditResult.Error);
        }

        return Result<(Movement, Movement)>.Success((debitResult.Value, creditResult.Value));
    }
}
