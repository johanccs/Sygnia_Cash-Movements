using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Writes movements. Idempotency on (AccountId, ExternalRef) is enforced by the database, not
/// here — the implementation attempts the INSERT and turns SQL error 2627/2601 into the
/// OK-replay / ALREADY_EXISTS outcomes described in the root CLAUDE.md; it never lets the SQL
/// exception escape as an exception.
/// <para>
/// One extra error code beyond the root CLAUDE.md's status-mapping table:
/// <c>movement.conflict_unresolved</c>, from <c>Sygnia.Infrastructure.MovementRepository</c>.
/// It fires when a key conflict was detected but the row can't be read back afterwards (a
/// concurrent delete, or — for a transfer — the leg genuinely never existed because the *other*
/// leg was the actual conflict and the pair rolled back together). Whoever builds the gRPC
/// error interceptor needs to map this; <c>INTERNAL</c> is the reasonable default, since it
/// represents an unresolvable state rather than an expected business outcome.
/// </para>
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

    /// <summary>Whether an account with this id exists, for NOT_FOUND checks before writing.</summary>
    Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken);
}
