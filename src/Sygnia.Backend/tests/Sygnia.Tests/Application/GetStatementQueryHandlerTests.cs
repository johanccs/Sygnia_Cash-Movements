using Sygnia.Application.Queries.GetStatement;
using Sygnia.Domain.Models;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class GetStatementQueryHandlerTests
{
    private const string AccountId = "ACC-001";

    private static Movement CreateMovement(decimal amount, DateTime occurredAt) => new(
        AccountId,
        $"MOV-{occurredAt:yyyyMMddHHmmss}",
        "ZAR",
        amount,
        occurredAt,
        null,
        Guid.NewGuid(),
        "jsmith",
        occurredAt);

    [Fact]
    public async Task Handle_UnknownAccount_YieldsSingleErrorLine()
    {
        var handler = new GetStatementQueryHandler(new FakeStatementReader(), new GetStatementQueryValidator());
        var query = new GetStatementQuery("ACC-404", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        var lines = await Collect(handler.Handle(query, CancellationToken.None));

        var line = Assert.Single(lines);
        Assert.NotNull(line.Error);
        Assert.Equal("account.not_found", line.Error!.Code);
    }

    [Fact]
    public async Task Handle_InvalidDateRange_YieldsSingleErrorLine()
    {
        var handler = new GetStatementQueryHandler(new FakeStatementReader(), new GetStatementQueryValidator());
        var query = new GetStatementQuery(AccountId, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)); // From after To

        var lines = await Collect(handler.Handle(query, CancellationToken.None));

        var line = Assert.Single(lines);
        Assert.NotNull(line.Error);
        Assert.Equal("statement.invalid", line.Error!.Code);
    }

    [Fact]
    public async Task Handle_KnownAccount_AccumulatesRunningTotalAcrossMovements()
    {
        var day1 = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var day2 = day1.AddDays(1);
        var reader = new FakeStatementReader { ExistingAccountIds = { AccountId } };
        reader.Movements.Add(CreateMovement(1000.00m, day1));
        reader.Movements.Add(CreateMovement(-250.00m, day2));

        var handler = new GetStatementQueryHandler(reader, new GetStatementQueryValidator());
        var query = new GetStatementQuery(AccountId, day1, day2);

        var lines = await Collect(handler.Handle(query, CancellationToken.None));

        Assert.Equal(2, lines.Count);
        Assert.Null(lines[0].Error);
        Assert.Equal(1000.00m, lines[0].RunningTotal);
        Assert.Equal(750.00m, lines[1].RunningTotal);
    }

    private static async Task<List<StatementLine>> Collect(IAsyncEnumerable<StatementLine> source)
    {
        var lines = new List<StatementLine>();
        await foreach (var line in source)
        {
            lines.Add(line);
        }

        return lines;
    }
}
