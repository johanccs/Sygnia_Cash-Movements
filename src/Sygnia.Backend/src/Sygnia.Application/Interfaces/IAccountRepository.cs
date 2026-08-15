using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>
/// Reads and creates accounts. <see cref="AccountId"/> is the primary key, so
/// <see cref="CreateAsync"/> reacts to a duplicate-key violation the same way
/// <see cref="IMovementRepository.AddAsync"/> does — never a SELECT-then-INSERT.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Returns the account, or <c>null</c> if it does not exist.</summary>
    Task<Account?> GetAsync(string accountId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new account. A duplicate <c>AccountId</c> comes back as
    /// <c>account.already_exists</c> rather than an exception.
    /// </summary>
    Task<Result<Account>> CreateAsync(Account account, CancellationToken cancellationToken);

    /// <summary>Returns every account, ordered by <see cref="Account.AccountId"/>.</summary>
    Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken);
}
