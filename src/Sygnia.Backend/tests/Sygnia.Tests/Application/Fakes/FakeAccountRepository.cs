using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Tests.Application.Fakes;

/// <summary>In-memory stand-in for account lookups, so handler tests don't need a database.</summary>
public sealed class FakeAccountRepository : IAccountRepository
{
    private readonly Dictionary<string, Account> _stored = new();

    public void AddExisting(string accountId, string currency = "ZAR") =>
        _stored[accountId] = new Account(accountId, "Test Account", null, currency, DateTime.UtcNow, "seed");

    public Task<Account?> GetAsync(string accountId, CancellationToken cancellationToken) =>
        Task.FromResult(_stored.GetValueOrDefault(accountId));

    public Task<Result<Account>> CreateAsync(Account account, CancellationToken cancellationToken)
    {
        if (!_stored.TryAdd(account.AccountId, account))
        {
            return Task.FromResult(Result<Account>.Failure(new Error(
                "account.already_exists",
                $"Account '{account.AccountId}' already exists.")));
        }

        return Task.FromResult(Result<Account>.Success(account));
    }

    public Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Account>>(_stored.Values.OrderBy(a => a.AccountId).ToList());
}
