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
internal sealed class UserRepository(SygniaDbContext db) : IUserRepository
{
    public async Task<User?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var entity = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

        return entity?.ToDomain();
    }

    public async Task<Result<User>> CreateAsync(User user, CancellationToken cancellationToken)
    {
        var entity = user.ToEntity();
        db.Users.Add(entity);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return Result<User>.Success(user);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKeyViolation())
        {
            return Result<User>.Failure(new Error(
                "user.already_exists",
                $"User '{user.Id}' already exists."));
        }
    }
}
