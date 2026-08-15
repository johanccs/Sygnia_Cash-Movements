using Sygnia.Application.Queries.GetStatementPage;
using Sygnia.Domain.Models;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class GetStatementPageQueryHandlerTests
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
    public async Task Handle_UnknownAccount_ReturnsNotFound()
    {
        var handler = new GetStatementPageQueryHandler(new FakeStatementReader(), new GetStatementPageQueryValidator());
        var query = new GetStatementPageQuery("ACC-404", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow, 1, 2);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_InvalidDateRange_ReturnsInvalid()
    {
        var handler = new GetStatementPageQueryHandler(new FakeStatementReader(), new GetStatementPageQueryValidator());
        var query = new GetStatementPageQuery(AccountId, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1), 1, 2);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("statement.invalid", result.Error.Code);
    }

    [Fact]
    public async Task Handle_Page1Of2_ReturnsTwoRowsAndTotalCountThree()
    {
        var day1 = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        var reader = new FakeStatementReader { ExistingAccountIds = { AccountId } };
        reader.Movements.Add(CreateMovement(100.00m, day1));
        reader.Movements.Add(CreateMovement(200.00m, day1.AddMinutes(1)));
        reader.Movements.Add(CreateMovement(300.00m, day1.AddMinutes(2)));

        var handler = new GetStatementPageQueryHandler(reader, new GetStatementPageQueryValidator());
        var query = new GetStatementPageQuery(AccountId, day1, day1.AddDays(1), 1, 2);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Rows.Count);
        Assert.Equal(3, result.Value.TotalCount);
    }
}
