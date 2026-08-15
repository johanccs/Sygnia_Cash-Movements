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
}
