using Sygnia.Application.Interfaces;
using Sygnia.Domain;

namespace Sygnia.Tests.Application.Fakes;

public sealed class FakeBalanceReader : IBalanceReader
{
    public Dictionary<string, decimal> Balances { get; } = new();

    public Task<Result<decimal>> GetBalanceAsync(string accountId, CancellationToken cancellationToken) =>
        Task.FromResult(Balances.TryGetValue(accountId, out var balance)
            ? Result<decimal>.Success(balance)
            : Result<decimal>.Failure(new Error("account.not_found", $"Account '{accountId}' does not exist.")));
}
