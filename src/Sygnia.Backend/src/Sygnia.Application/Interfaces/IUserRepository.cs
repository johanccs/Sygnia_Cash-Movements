using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Application.Interfaces;

/// <summary>Reads and creates operations users, whose id lands in <c>MovedBy</c>/<c>CreatedBy</c>.</summary>
public interface IUserRepository
{
    /// <summary>Returns the user, or <c>null</c> if it does not exist.</summary>
    Task<User?> GetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts a new user. A duplicate <c>Id</c> comes back as <c>user.already_exists</c>
    /// rather than an exception.
    /// </summary>
    Task<Result<User>> CreateAsync(User user, CancellationToken cancellationToken);
}
