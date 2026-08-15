using System.Runtime.CompilerServices;
using Sygnia.Application.Interfaces;
using Sygnia.Domain.Models;

namespace Sygnia.Tests.Application.Fakes;

public sealed class FakeStatementReader : IStatementReader
{
    public HashSet<string> ExistingAccountIds { get; } = new();
    public List<Movement> Movements { get; } = new();

    public Task<bool> AccountExistsAsync(string accountId, CancellationToken cancellationToken) =>
        Task.FromResult(ExistingAccountIds.Contains(accountId));

    public async IAsyncEnumerable<Movement> StreamAsync(
        string accountId,
        DateTime from,
        DateTime to,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var movement in Movements
            .Where(m => m.AccountId == accountId && m.OccurredAt >= from && m.OccurredAt <= to)
            .OrderBy(m => m.OccurredAt))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return movement;
            await Task.Yield();
        }
    }

    public Task<(IReadOnlyList<Movement> Rows, int TotalCount)> GetPageAsync(
        string accountId, DateTime from, DateTime to, int pageNumber, int pageSize, CancellationToken cancellationToken)
    {
        var matching = Movements
            .Where(m => m.AccountId == accountId && m.OccurredAt >= from && m.OccurredAt <= to)
            .OrderBy(m => m.OccurredAt)
            .ToList();

        var rows = matching
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Task.FromResult<(IReadOnlyList<Movement> Rows, int TotalCount)>((rows, matching.Count));
    }
}
