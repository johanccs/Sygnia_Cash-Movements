using Microsoft.EntityFrameworkCore;
using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Infrastructure.Persistence;

namespace Sygnia.Infrastructure.Repositories;

/// <summary>
/// Balance is a SUM(amount) aggregate computed on read — no materialised balance column, per
/// the root CLAUDE.md. <c>IX_Movements_Account_OccurredAt INCLUDE (Amount)</c> serves this
/// query from the index alone.
/// </summary>
internal sealed class BalanceReader(SygniaDbContext db) : IBalanceReader
{
    public async Task<Result<decimal>> GetBalanceAsync(string accountId, CancellationToken cancellationToken)
    {
        if (!await db.Accounts.AsNoTracking().AnyAsync(a => a.AccountId == accountId, cancellationToken))
        {
            return Result<decimal>.Failure(new Error("account.not_found", $"Account '{accountId}' does not exist."));
        }

        var balance = await db.Movements
            .AsNoTracking()
            .Where(m => m.AccountId == accountId)
            .SumAsync(m => m.Amount, cancellationToken);

        return Result<decimal>.Success(balance);
    }
}
