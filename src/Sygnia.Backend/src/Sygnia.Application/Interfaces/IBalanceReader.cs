using Sygnia.Domain;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Computes an account's balance as SUM(amount) on read — no materialised balance column.
/// </summary>
public interface IBalanceReader
{
    /// <summary>Failure carries a NOT_FOUND-shaped <see cref="Error"/> for an unknown account.</summary>
    Task<Result<decimal>> GetBalanceAsync(string accountId, CancellationToken cancellationToken);
}
