using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Repositories;

namespace Sygnia.Tests.Infrastructure.Integration;

/// <summary>
/// <see cref="MovementRepository.AddTransferAsync"/> writes both legs in one
/// <c>SaveChangesAsync</c>, so a duplicate-key conflict on either leg rolls the whole pair
/// back. Resolving that conflict has to check both legs independently rather than
/// short-circuiting on whichever is checked first — a leg reading back "not found" after the
/// rollback is expected when the *other* leg was the actual conflict, not an error in its own
/// right.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class MovementRepositoryTransferConflictTests(SqlServerFixture fixture)
{
    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Fact]
    public async Task AddTransferAsync_SameTransferReplayed_BothLegsSucceedAsTheStoredMovements()
    {
        var fromAccount = $"ACC-{Guid.NewGuid():N}"[..10];
        var toAccount = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(fromAccount);
        await fixture.SeedAccountAsync(toAccount);

        var debit = CreateMovement(fromAccount, "MOV-XFER-01-DR", -500.00m);
        var credit = CreateMovement(toAccount, "MOV-XFER-01-CR", 500.00m);

        // A fresh context per call, matching production: MovementRepository is scoped per
        // request, so no single DbContext ever sees two AddTransferAsync calls for the same
        // key. Reusing one context here would hit an unrelated EF identity-tracking conflict
        // that has nothing to do with the behaviour under test.
        var first = await new MovementRepository(fixture.CreateContext()).AddTransferAsync(debit, credit, CancellationToken.None);
        var second = await new MovementRepository(fixture.CreateContext()).AddTransferAsync(debit, credit, CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.Debit.Amount, second.Value.Debit.Amount);
        Assert.Equal(first.Value.Credit.Amount, second.Value.Credit.Amount);
    }

    [Fact]
    public async Task AddTransferAsync_OnlyDebitLegConflicts_ReportsAlreadyExistsNamingDebitOnly()
    {
        var fromAccount = $"ACC-{Guid.NewGuid():N}"[..10];
        var firstToAccount = $"ACC-{Guid.NewGuid():N}"[..10];
        var secondToAccount = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(fromAccount);
        await fixture.SeedAccountAsync(firstToAccount);
        await fixture.SeedAccountAsync(secondToAccount);

        // First transfer establishes the debit leg's key (fromAccount, "MOV-XFER-02-DR").
        var originalDebit = CreateMovement(fromAccount, "MOV-XFER-02-DR", -100.00m);
        var originalCredit = CreateMovement(firstToAccount, "MOV-XFER-02-CR", 100.00m);
        var original = await new MovementRepository(fixture.CreateContext())
            .AddTransferAsync(originalDebit, originalCredit, CancellationToken.None);
        Assert.True(original.IsSuccess);

        // Second transfer reuses the same ExternalRef and FromAccount (so the debit leg's key
        // collides) but a different ToAccount and amount, so the credit leg's key is entirely
        // new and was never written by either call — it must not be reported as a conflict.
        // Fresh context, matching production's per-request scoping.
        var conflictingDebit = CreateMovement(fromAccount, "MOV-XFER-02-DR", -250.00m);
        var newCredit = CreateMovement(secondToAccount, "MOV-XFER-02-CR", 250.00m);
        var conflicting = await new MovementRepository(fixture.CreateContext())
            .AddTransferAsync(conflictingDebit, newCredit, CancellationToken.None);

        Assert.True(conflicting.IsFailure);
        Assert.Equal("movement.already_exists", conflicting.Error.Code);
        Assert.Contains("debit", conflicting.Error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("credit", conflicting.Error.Message, StringComparison.OrdinalIgnoreCase);

        // The credit leg that was never actually in conflict must not have been written either
        // — the pair stays atomic even while resolving the conflict.
        await using var db = fixture.CreateContext();
        var newCreditRowCount = db.Movements.Count(m => m.AccountId == secondToAccount && m.ExternalRef == "MOV-XFER-02-CR");
        Assert.Equal(0, newCreditRowCount);
    }

    private static Movement CreateMovement(string accountId, string externalRef, decimal amount) => new(
        accountId,
        externalRef,
        "ZAR",
        amount,
        OccurredAt,
        "Transfer",
        Guid.NewGuid(),
        "jsmith",
        OccurredAt);
}
