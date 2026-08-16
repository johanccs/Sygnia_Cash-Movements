using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
/// so <see cref="AddTransferAsync"/> writes both legs atomically for free — which also means a
/// conflict on either leg rolls the whole pair back, so the other leg reading back "not found"
/// afterwards is expected, not itself a conflict. See <see cref="ResolveTransferConflictAsync"/>.
/// A genuine field conflict (not an identical replay) is never silent: it is reported to the
/// caller as <c>ALREADY_EXISTS</c> AND persisted to <c>MovementConflictAudits</c> — see
/// <see cref="AuditConflictAsync"/>.
/// </summary>
internal sealed class MovementRepository(SygniaDbContext db, ILogger<MovementRepository> logger) : IMovementRepository
{
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
            var stored = await FindStoredAsync(movement.AccountId, movement.ExternalRef, cancellationToken);
            var result = ResolveSingleLegConflict(movement, stored);
            if (result.IsFailure && stored is not null)
            {
                await AuditConflictAsync(movement, stored, cancellationToken);
            }

            return result;
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
            return await ResolveTransferConflictAsync(debit, credit, cancellationToken);
        }
    }

    /// <summary>
    /// Resolves both legs independently before deciding anything — never short-circuits on
    /// whichever leg is checked first. Because the failed <c>SaveChangesAsync</c> rolled the
    /// whole pair back, a leg reading back "not found" only means *that* leg was never the
    /// cause of the conflict; it must not be reported as one.
    /// </summary>
    private async Task<Result<(Movement Debit, Movement Credit)>> ResolveTransferConflictAsync(
        Movement debit,
        Movement credit,
        CancellationToken cancellationToken)
    {
        var storedDebit = await FindStoredAsync(debit.AccountId, debit.ExternalRef, cancellationToken);
        var storedCredit = await FindStoredAsync(credit.AccountId, credit.ExternalRef, cancellationToken);

        var conflictMessages = DescribeLegConflicts(debit, storedDebit, credit, storedCredit);
        if (conflictMessages.Count > 0)
        {
            return Result<(Movement, Movement)>.Failure(new Error(
                ErrorCode.MovementAlreadyExists,
                $"Transfer '{debit.RefNr}' already exists with a different {string.Join(", ", conflictMessages)}."));
        }

        return ResolveNoConflict(debit.RefNr, storedDebit, storedCredit);
    }

    // A leg that was actually found with mismatched fields is a genuine, reportable conflict
    // regardless of what the other leg looks like.
    private static List<string> DescribeLegConflicts(
        Movement debit, MovementEntity? storedDebit, Movement credit, MovementEntity? storedCredit)
    {
        var messages = new List<string>();

        var debitConflicts = storedDebit is null ? null : DescribeConflicts(debit, storedDebit);
        if (debitConflicts is { Count: > 0 })
        {
            messages.Add($"debit {string.Join("/", debitConflicts)}");
        }

        var creditConflicts = storedCredit is null ? null : DescribeConflicts(credit, storedCredit);
        if (creditConflicts is { Count: > 0 })
        {
            messages.Add($"credit {string.Join("/", creditConflicts)}");
        }

        return messages;
    }

    // No mismatches found. Either both legs are an identical replay (the normal case for the
    // same transfer resubmitted), or one leg was never written at all — which cannot be
    // resolved as a replay, since this repository never writes a transfer's legs apart.
    private static Result<(Movement Debit, Movement Credit)> ResolveNoConflict(
        Guid refNr, MovementEntity? storedDebit, MovementEntity? storedCredit)
    {
        if (storedDebit is not null && storedCredit is not null)
        {
            return Result<(Movement, Movement)>.Success((storedDebit.ToDomain(), storedCredit.ToDomain()));
        }

        var missingLeg = storedDebit is null ? "debit" : "credit";
        return Result<(Movement, Movement)>.Failure(new Error(
            ErrorCode.MovementConflictUnresolved,
            $"Transfer '{refNr}' hit a key conflict but its {missingLeg} leg could not be read back."));
    }

    private static Result<Movement> ResolveSingleLegConflict(Movement attempted, MovementEntity? stored)
    {
        if (stored is null)
        {
            // The key we just failed to insert is now missing — a concurrent delete. Not a
            // conflict we can resolve as a replay.
            return Result<Movement>.Failure(new Error(
                ErrorCode.MovementConflictUnresolved,
                $"'{attempted.ExternalRef}' for '{attempted.AccountId}' could not be read back after a key conflict."));
        }

        var conflicts = DescribeConflicts(attempted, stored);
        return conflicts.Count == 0
            ? Result<Movement>.Success(stored.ToDomain())
            : Result<Movement>.Failure(new Error(
                ErrorCode.MovementAlreadyExists,
                $"'{attempted.ExternalRef}' already exists for '{attempted.AccountId}' with a different {string.Join(", ", conflicts)}."));
    }

    /// <summary>
    /// Persists the conflict and logs a warning. A separate <c>SaveChangesAsync</c> from the
    /// failed write above — that transaction is already rolled back, so this is its own attempt
    /// and is deliberately best-effort: a failure to audit must not turn an otherwise-handled
    /// conflict into an unhandled exception on the caller's request.
    /// </summary>
    private async Task AuditConflictAsync(Movement attempted, MovementEntity stored, CancellationToken cancellationToken)
    {
        var conflictingFields = string.Join(", ", DescribeConflicts(attempted, stored));

        logger.LogWarning(
            "Idempotency conflict for {AccountId}/{ExternalRef}: attempted submission differs from " +
            "the stored movement in {ConflictingFields}.",
            attempted.AccountId, attempted.ExternalRef, conflictingFields);

        db.MovementConflictAudits.Add(new MovementConflictAuditEntity
        {
            AccountId = attempted.AccountId,
            ExternalRef = attempted.ExternalRef,
            AttemptedAmount = attempted.Amount,
            AttemptedCurrency = attempted.Currency,
            AttemptedOccurredAt = attempted.OccurredAt,
            StoredAmount = stored.Amount,
            StoredCurrency = stored.Currency,
            StoredOccurredAt = stored.OccurredAt,
            ConflictingFields = conflictingFields,
            DetectedAt = DateTime.UtcNow,
        });

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Failed to persist conflict audit row for {AccountId}/{ExternalRef}.",
                attempted.AccountId, attempted.ExternalRef);
        }
    }

    private Task<MovementEntity?> FindStoredAsync(string accountId, string externalRef, CancellationToken cancellationToken) =>
        db.Movements
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.AccountId == accountId && m.ExternalRef == externalRef, cancellationToken);

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
