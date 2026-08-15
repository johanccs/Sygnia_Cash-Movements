using Sygnia.Application.Queries.GetBalance;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class GetBalanceQueryHandlerTests
{
    [Fact]
    public async Task Handle_KnownAccount_ReturnsSum()
    {
        var reader = new FakeBalanceReader();
        reader.Balances["ACC-001"] = 12500.00m;
        var handler = new GetBalanceQueryHandler(reader, new GetBalanceQueryValidator());

        var result = await handler.Handle(new GetBalanceQuery("ACC-001"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12500.00m, result.Value);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsNotFoundFailure()
    {
        var handler = new GetBalanceQueryHandler(new FakeBalanceReader(), new GetBalanceQueryValidator());

        var result = await handler.Handle(new GetBalanceQuery("ACC-404"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_BlankAccountId_ReturnsValidationFailure()
    {
        var handler = new GetBalanceQueryHandler(new FakeBalanceReader(), new GetBalanceQueryValidator());

        var result = await handler.Handle(new GetBalanceQuery(""), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("balance.invalid", result.Error.Code);
    }
}
