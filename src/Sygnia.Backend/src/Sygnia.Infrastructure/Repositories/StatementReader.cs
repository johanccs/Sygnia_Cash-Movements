using Microsoft.EntityFrameworkCore;
using Sygnia.Application.Interfaces;
using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Mapping;
using Sygnia.Infrastructure.Persistence;

namespace Sygnia.Infrastructure.Repositories;

/// <summary>
/// Streams end to end: <c>AsAsyncEnumerable()</c> + <c>AsNoTracking()</c>, ordered so the
/// caller can accumulate a running total. A <c>.ToListAsync()</c> anywhere here defeats the
/// 50k-row streaming requirement while every functional test still passes.
/// </summary>
internal sealed class StatementReader(SygniaDbContext db) : IStatementReader
{
    public Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().AnyAsync(a => a.AccountId == accountId, cancellationToken);

    public async IAsyncEnumerable<Movement> StreamAsync(
        string accountId,
        DateTime from,
        DateTime to,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var query = db.Movements
            .AsNoTracking()
            .Where(m => m.AccountId == accountId && m.OccurredAt >= from && m.OccurredAt <= to)
            .OrderBy(m => m.OccurredAt)
            .AsAsyncEnumerable();

        await foreach (var entity in query.WithCancellation(cancellationToken))
        {
            yield return entity.ToDomain();
        }
    }

    /// <summary>
    /// A single page for the UI's paginated table — buffered on purpose, unlike
    /// <see cref="StreamAsync"/>'s full stream. This is the one intentional
    /// <c>ToListAsync()</c> in the statement path: it is scoped to a single page, not the full
    /// result set, so it does not defeat the streaming requirement (which <see cref="StreamAsync"/>
    /// alone must satisfy).
    /// </summary>
    public async Task<(IReadOnlyList<Movement> Rows, int TotalCount)> GetPageAsync(
        string accountId, DateTime from, DateTime to, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Movements.AsNoTracking()
            .Where(m => m.AccountId == accountId && m.OccurredAt >= from && m.OccurredAt <= to);

        var total = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderBy(m => m.OccurredAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyList<Movement> rows = entities.Select(e => e.ToDomain()).ToList();

        return (rows, total);
    }
}
