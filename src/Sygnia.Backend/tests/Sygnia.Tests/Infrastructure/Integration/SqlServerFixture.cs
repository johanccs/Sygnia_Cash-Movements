using Microsoft.EntityFrameworkCore;
using Sygnia.Infrastructure.Entities;
using Sygnia.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Sygnia.Tests.Infrastructure.Integration;

/// <summary>
/// One SQL Server container per test class (xUnit's <see cref="IClassFixture{TFixture}"/>
/// contract), migrated once. Requires Docker; these are integration tests, not unit tests.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public SygniaDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SygniaDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .Options;

        return new SygniaDbContext(options);
    }

    /// <summary>Inserts an account directly, bypassing the repositories under test.</summary>
    public async Task SeedAccountAsync(string accountId, string currency = "ZAR")
    {
        await using var db = CreateContext();
        db.Accounts.Add(new AccountEntity
        {
            AccountId = accountId,
            AccountName = "Test Account",
            ContactPerson = null,
            Currency = currency,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "test",
        });
        await db.SaveChangesAsync();
    }
}
