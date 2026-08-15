using Sygnia.Application.Interfaces;
using Sygnia.Domain;
using Sygnia.Domain.Models;

namespace Sygnia.Tests.Application.Fakes;

/// <summary>In-memory stand-in for user lookups, so handler tests don't need a database.</summary>
public sealed class FakeUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _stored = new();

    public void AddExisting(string id, string name = "Test", string surname = "User") =>
        _stored[id] = new User(id, name, surname);

    public Task<User?> GetAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_stored.GetValueOrDefault(id));

    public Task<Result<User>> CreateAsync(User user, CancellationToken cancellationToken)
    {
        if (!_stored.TryAdd(user.Id, user))
        {
            return Task.FromResult(Result<User>.Failure(new Error(
                "user.already_exists",
                $"User '{user.Id}' already exists.")));
        }

        return Task.FromResult(Result<User>.Success(user));
    }
}
