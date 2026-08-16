using Microsoft.EntityFrameworkCore;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Mapping;
using Sygnia.Infrastructure.Persistence;

namespace Sygnia.Infrastructure.Repositories;

/// <summary>
/// Same idempotency shape as <see cref="MovementRepository"/>: attempt the INSERT first and
/// only react to SQL error 2627/2601 afterwards — never a SELECT-then-INSERT.
/// </summary>
internal sealed class AccountRepository(SygniaDbContext db) : IAccountRepository
{
    public async Task<Account?> GetAsync(string accountId, CancellationToken cancellationToken)
    {
        var entity = await db.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<Result<Account>> CreateAsync(Account account, CancellationToken cancellationToken)
    {
        var entity = account.ToEntity();
        db.Accounts.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result<Account>.Success(account);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            return Result<Account>.Failure(new Error(
                ErrorCode.AccountAlreadyExists,
                $"Account '{account.AccountId}' already exists."));
        }
    }

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        var entities = await db.Accounts
            .AsNoTracking()
            .OrderBy(a => a.AccountId)
            .ToListAsync(cancellationToken);

        return entities.Select(e => e.ToDomain()).ToList();
    }
}
