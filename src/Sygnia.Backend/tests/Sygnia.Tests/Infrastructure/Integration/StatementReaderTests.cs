using Microsoft.EntityFrameworkCore;
using Sygnia.Infrastructure.Repositories;

namespace Sygnia.Tests.Infrastructure.Integration;

/// <summary>
/// <see cref="StatementReader.StreamAsync"/> is required to stream via
/// <c>AsAsyncEnumerable()</c>/<c>AsNoTracking()</c> end to end, per the root CLAUDE.md — a
/// <c>.ToListAsync()</c> anywhere in the path would defeat that while every functional test
/// still passes. These tests check functional correctness (ordering, running-total inputs,
/// completeness at scale); they do not themselves prove memory stays flat, since that would
/// need instrumentation this suite doesn't have.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public sealed class StatementReaderTests(SqlServerFixture fixture)
{
    private static readonly DateTime Day1 = new(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task StreamAsync_UnknownAccount_ReturnsFalseFromAccountExists()
    {
        var reader = new StatementReader(fixture.CreateContext());

        var exists = await reader.AccountExistsAsync($"ACC-{Guid.NewGuid():N}"[..10], CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task StreamAsync_MovementsInRange_YieldsThemOrderedByOccurredAt()
    {
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);
        await using (var db = fixture.CreateContext())
        {
            db.Movements.AddRange(
                new Sygnia.Infrastructure.Entities.MovementEntity
                {
                    AccountId = accountId, ExternalRef = "MOV-STM-02", Currency = "ZAR",
                    Amount = -100m, OccurredAt = Day1.AddDays(1), RefNr = Guid.NewGuid(), MovedBy = "jsmith", MovedDate = Day1,
                },
                new Sygnia.Infrastructure.Entities.MovementEntity
                {
                    AccountId = accountId, ExternalRef = "MOV-STM-01", Currency = "ZAR",
                    Amount = 500m, OccurredAt = Day1, RefNr = Guid.NewGuid(), MovedBy = "jsmith", MovedDate = Day1,
                });
            await db.SaveChangesAsync();
        }

        var reader = new StatementReader(fixture.CreateContext());
        var movements = new List<Sygnia.Domain.Models.Movement>();
        await foreach (var movement in reader.StreamAsync(accountId, Day1, Day1.AddDays(2), CancellationToken.None))
        {
            movements.Add(movement);
        }

        Assert.Equal(2, movements.Count);
        Assert.Equal("MOV-STM-01", movements[0].ExternalRef); // earlier OccurredAt first
        Assert.Equal("MOV-STM-02", movements[1].ExternalRef);
    }

    [Fact]
    public async Task StreamAsync_50000Movements_YieldsEveryRowWithCorrectFinalRunningTotal()
    {
        const int rowCount = 50_000;
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);

        await using (var db = fixture.CreateContext())
        {
            // Set-based insert — 50,000 individual EF Core Add() calls would make this test
            // about the insert, not the stream.
            await db.Database.ExecuteSqlRawAsync(
                """
                ;WITH Numbers AS (
                    SELECT TOP (@rowCount) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS N
                    FROM sys.all_objects a CROSS JOIN sys.all_objects b
                )
                INSERT INTO Movements (AccountId, ExternalRef, Currency, Amount, OccurredAt, Narration, RefNr, MovedBy, MovedDate)
                SELECT @accountId, CONCAT('MOV-STM-', RIGHT('00000' + CAST(N AS varchar(5)), 5)), 'ZAR',
                       1.00, DATEADD(SECOND, N, @day1), NULL, NEWID(), 'jsmith', @day1
                FROM Numbers
                """,
                new Microsoft.Data.SqlClient.SqlParameter("@rowCount", rowCount),
                new Microsoft.Data.SqlClient.SqlParameter("@accountId", accountId),
                new Microsoft.Data.SqlClient.SqlParameter("@day1", Day1));
        }

        var reader = new StatementReader(fixture.CreateContext());
        var count = 0;
        var runningTotal = 0m;
        await foreach (var movement in reader.StreamAsync(accountId, Day1, Day1.AddDays(1), CancellationToken.None))
        {
            count++;
            runningTotal += movement.Amount;
        }

        Assert.Equal(rowCount, count);
        Assert.Equal(rowCount * 1.00m, runningTotal);
    }
}
