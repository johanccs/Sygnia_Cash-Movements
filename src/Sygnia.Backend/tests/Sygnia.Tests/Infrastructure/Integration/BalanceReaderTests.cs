using Sygnia.Domain.Models;
using Sygnia.Infrastructure.Repositories;

namespace Sygnia.Tests.Infrastructure.Integration;

[Collection(nameof(SqlServerCollection))]
public sealed class BalanceReaderTests(SqlServerFixture fixture)
{
    private static readonly DateTime OccurredAt = new(2024, 7, 15, 10, 42, 31, DateTimeKind.Utc);

    [Fact]
    public async Task GetBalanceAsync_SumsMovementsForTheAccount()
    {
        var accountId = $"ACC-{Guid.NewGuid():N}"[..10];
        await fixture.SeedAccountAsync(accountId);

        var movementRepository = new MovementRepository(fixture.CreateContext());
        await movementRepository.AddAsync(
            new Movement(accountId, "MOV-BAL-01", "ZAR", 1000.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt),
            CancellationToken.None);
        await movementRepository.AddAsync(
            new Movement(accountId, "MOV-BAL-02", "ZAR", -250.00m, OccurredAt, null, Guid.NewGuid(), "jsmith", OccurredAt),
            CancellationToken.None);

        var balanceReader = new BalanceReader(fixture.CreateContext());
        var result = await balanceReader.GetBalanceAsync(accountId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(750.00m, result.Value);
    }

    [Fact]
    public async Task GetBalanceAsync_UnknownAccount_ReturnsNotFoundFailure()
    {
        var balanceReader = new BalanceReader(fixture.CreateContext());

        var result = await balanceReader.GetBalanceAsync($"ACC-{Guid.NewGuid():N}"[..10], CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }
}
