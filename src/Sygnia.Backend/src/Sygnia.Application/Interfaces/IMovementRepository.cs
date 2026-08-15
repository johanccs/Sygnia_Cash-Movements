using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Writes movements. Idempotency on (AccountId, ExternalRef) is enforced by the database, not
/// here — the implementation attempts the INSERT and turns SQL error 2627/2601 into the
/// OK-replay / ALREADY_EXISTS outcomes described in the root CLAUDE.md; it never lets the SQL
/// exception escape as an exception.
/// TODO: implement in Sygnia.Infrastructure against the EF Core DbContext (not yet created).
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
