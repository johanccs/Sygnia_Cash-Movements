using Microsoft.EntityFrameworkCore;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Entities;
using Sygnia.Infrastructure.Mapping;
using Sygnia.Infrastructure.Persistence;

namespace Sygnia.Infrastructure.Repositories;

/// <summary>
/// Idempotency lives here, not in application code: every write attempts the INSERT first and
/// only reacts to SQL error 2627/2601 afterwards — no SELECT-then-INSERT, which would lose the
/// race the brief tests for. A single <c>SaveChangesAsync</c> call is one implicit transaction,
/// so <see cref="AddTransferAsync"/> writes both legs atomically for free.
/// </summary>
internal sealed class MovementRepository(SygniaDbContext db) : IMovementRepository
{
    public Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().AnyAsync(a => a.AccountId == accountId, cancellationToken);

    public async Task<Result<Movement>> AddAsync(Movement movement, CancellationToken cancellationToken)
    {
        var entity = movement.ToEntity();
        db.Movements.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result<Movement>.Success(movement);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            db.Entry(entity).State = EntityState.Detached;
            return await ResolveConflictAsync(movement, cancellationToken);
        }
    }

    public async Task<Result<(Movement Debit, Movement Credit)>> AddTransferAsync(
        Movement debit,
        Movement credit,
        CancellationToken cancellationToken)
    {
        var debitEntity = debit.ToEntity();
        var creditEntity = credit.ToEntity();
        db.Movements.AddRange(debitEntity, creditEntity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result<(Movement, Movement)>.Success((debit, credit));
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            db.ChangeTracker.Clear();

            var debitResult = await ResolveConflictAsync(debit, cancellationToken);
            if (debitResult.IsFailure)
            {
                return Result<(Movement, Movement)>.Failure(debitResult.Error);
            }

            var creditResult = await ResolveConflictAsync(credit, cancellationToken);
            return creditResult.IsFailure
                ? Result<(Movement, Movement)>.Failure(creditResult.Error)
                : Result<(Movement, Movement)>.Success((debitResult.Value, creditResult.Value));
        }
    }

    /// <summary>
    /// Reads the stored row back for a key that just failed to insert, and compares it against
    /// what the caller attempted: identical amount/currency/occurredAt is an idempotent replay
    /// (OK, the stored row); anything else is ALREADY_EXISTS naming the conflicting fields.
    /// </summary>
    private async Task<Result<Movement>> ResolveConflictAsync(Movement attempted, CancellationToken cancellationToken)
    {
        var stored = await db.Movements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                m => m.AccountId == attempted.AccountId && m.ExternalRef == attempted.ExternalRef,
                cancellationToken);

        if (stored is null)
        {
            // The key we just failed to insert is now missing — a concurrent delete, or this
            // conflict belongs to the other leg of a transfer. Either way, this is not a
            // conflict we can resolve as a replay.
            return Result<Movement>.Failure(new Error(
                "movement.conflict_unresolved",
                $"'{attempted.ExternalRef}' for '{attempted.AccountId}' could not be read back after a key conflict."));
        }

        var conflicts = DescribeConflicts(attempted, stored);
        if (conflicts.Count == 0)
        {
            return Result<Movement>.Success(stored.ToDomain());
        }

        return Result<Movement>.Failure(new Error(
            "movement.already_exists",
            $"'{attempted.ExternalRef}' already exists for '{attempted.AccountId}' with a different {string.Join(", ", conflicts)}."));
    }

    private static List<string> DescribeConflicts(Movement attempted, MovementEntity stored)
    {
        var conflicts = new List<string>();
        if (stored.Amount != attempted.Amount)
        {
            conflicts.Add(nameof(Movement.Amount));
        }

        if (!string.Equals(stored.Currency, attempted.Currency, StringComparison.Ordinal))
        {
            conflicts.Add(nameof(Movement.Currency));
        }

        if (stored.OccurredAt != attempted.OccurredAt)
        {
            conflicts.Add(nameof(Movement.OccurredAt));
        }

        return conflicts;
    }
}
