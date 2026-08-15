using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Streams movements end to end — implementations must use EF Core's
/// <c>AsAsyncEnumerable()</c> + <c>AsNoTracking()</c>. A <c>.ToListAsync()</c> anywhere in this
/// path defeats the 50k-row streaming requirement while every functional test still passes.
/// TODO: implement in Sygnia.Infrastructure against the EF Core DbContext (not yet created).
/// </summary>
public interface IStatementReader
{
    Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken);

    /// <summary>Movements ordered by <c>OccurredAt</c>, for a running total to accumulate over.</summary>
    IAsyncEnumerable<Movement> StreamAsync(
        string accountId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken);
}
