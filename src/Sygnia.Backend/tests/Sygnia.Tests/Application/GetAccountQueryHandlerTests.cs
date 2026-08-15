using Sygnia.Application.Queries.GetAccount;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class GetAccountQueryHandlerTests
{
    private const string AccountId = "ACC-001";

    [Fact]
    public async Task Handle_ExistingAccount_ReturnsSuccess()
    {
        var repository = new FakeAccountRepository();
        repository.AddExisting(AccountId);
        var handler = new GetAccountQueryHandler(repository);

        var result = await handler.Handle(new GetAccountQuery(AccountId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(AccountId, result.Value.AccountId);
    }

    [Fact]
    public async Task Handle_UnknownAccount_ReturnsNotFoundFailure()
    {
        var repository = new FakeAccountRepository();
        var handler = new GetAccountQueryHandler(repository);

        var result = await handler.Handle(new GetAccountQuery(AccountId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("account.not_found", result.Error.Code);
    }
}
