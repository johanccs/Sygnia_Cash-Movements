using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Writes movements. Idempotency on (AccountId, ExternalRef) is enforced by the database, not
/// here — the implementation attempts the INSERT and turns SQL error 2627/2601 into the
/// OK-replay / ALREADY_EXISTS outcomes described in the root CLAUDE.md. Also emits
/// <c>movement.conflict_unresolved</c> (map to <c>INTERNAL</c>) when a key conflict is detected
/// but the row can't be read back — e.g. a concurrent delete, or a transfer leg that never
/// existed because its pair rolled back together.
/// </summary>
public interface IMovementRepository
{
    /// <summary>Inserts a single movement against an existing account.</summary>
    Task<Result<Movement>> AddAsync(Movement movement, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts both legs of a transfer in a single atomic transaction. Idempotency applies to
    /// each leg's (AccountId, ExternalRef) independently.
    /// </summary>
    Task<Result<(Movement Debit, Movement Credit)>> AddTransferAsync(
        Movement debit,
        Movement credit,
        CancellationToken cancellationToken);
}
