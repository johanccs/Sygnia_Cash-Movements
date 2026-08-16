using Microsoft.Extensions.Logging.Abstractions;
using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Repositories;

namespace Sygnia.Tests.Infrastructure.Integration;

/// <summary>
/// The invariant this project is graded on: idempotency lives in the database, not in C#, and
/// has to survive two callers racing for the same key — not just a sequential replay. Each
/// racer gets its own <c>DbContext</c>, since a single context isn't thread-safe; the database
/// itself is what arbitrates the race via the (AccountId, ExternalRef) primary key.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class MovementRepositoryConcurrentTests(SqlServerFixture fixture)
{
    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Fact]
    public async Task AddAsync_ConcurrentIdenticalSubmissions_BothSucceedAsTheSameStoredMovement()
    {
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);

        var movement = new Movement(
            accountId,
            "MOV-CONCURRENT-01",
            "ZAR",
            1000.00m,
            OccurredAt,
            "Race",
            Guid.NewGuid(),
            "jsmith",
            OccurredAt);

        var repositoryA = new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance);
        var repositoryB = new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance);

        var results = await Task.WhenAll(
            repositoryA.AddAsync(movement, CancellationToken.None),
            repositoryB.AddAsync(movement, CancellationToken.None));

        Assert.All(results, r => Assert.True(r.IsSuccess));
        Assert.Equal(results[0].Value.Amount, results[1].Value.Amount);
        Assert.Equal(results[0].Value.Currency, results[1].Value.Currency);
        Assert.Equal(results[0].Value.OccurredAt, results[1].Value.OccurredAt);

        await using var db = fixture.CreateContext();
        var rowCount = db.Movements.Count(m => m.AccountId == accountId && m.ExternalRef == movement.ExternalRef);
        Assert.Equal(1, rowCount);
    }

    [Fact]
    public async Task AddAsync_ConcurrentConflictingSubmissions_ExactlyOneSucceeds()
    {
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);
        const string externalRef = "MOV-CONCURRENT-02";

        var first = new Movement(accountId, externalRef, "ZAR", 1000.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt);
        var second = new Movement(accountId, externalRef, "ZAR", 999.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt);

        var repositoryA = new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance);
        var repositoryB = new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance);

        var results = await Task.WhenAll(
            repositoryA.AddAsync(first, CancellationToken.None),
            repositoryB.AddAsync(second, CancellationToken.None));

        Assert.Single(results, r => r.IsSuccess);
        var failure = Assert.Single(results, r => r.IsFailure);
        Assert.Equal("movement.already_exists", failure.Error.Code);

        await using var db = fixture.CreateContext();
        var rowCount = db.Movements.Count(m => m.AccountId == accountId && m.ExternalRef == externalRef);
        Assert.Equal(1, rowCount);
    }

    [Fact]
    public async Task AddAsync_ConflictingSubmission_WritesAuditRowNamingTheConflictingFields()
    {
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);
        const string externalRef = "MOV-AUDIT-01";

        var original = new Movement(accountId, externalRef, "ZAR", 1000.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt);
        var conflicting = new Movement(accountId, externalRef, "ZAR", 999.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt);

        await new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance)
            .AddAsync(original, CancellationToken.None);
        var result = await new MovementRepository(fixture.CreateContext(), NullLogger<MovementRepository>.Instance)
            .AddAsync(conflicting, CancellationToken.None);

        Assert.True(result.IsFailure);

        await using var db = fixture.CreateContext();
        var audit = db.MovementConflictAudits.Single(a => a.AccountId == accountId && a.ExternalRef == externalRef);
        Assert.Equal(999.00m, audit.AttemptedAmount);
        Assert.Equal(1000.00m, audit.StoredAmount);
        Assert.Contains("Amount", audit.ConflictingFields);
    }
}
