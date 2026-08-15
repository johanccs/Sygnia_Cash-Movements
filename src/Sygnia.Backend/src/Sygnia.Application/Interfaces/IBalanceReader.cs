using Sygnia.Domain;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Computes an account's balance as SUM(amount) on read — no materialised balance column.
/// TODO: implement in Sygnia.Infrastructure against the EF Core DbContext (not yet created).
/// </summary>
public interface IBalanceReader
{
    /// <summary>Failure carries a NOT_FOUND-shaped <see cref="Error"/> for an unknown account.</summary>
    Task<Result<decimal>> GetBalanceAsync(string accountId, CancellationToken cancellationToken);
}
