using Sygnia.Application.Queries.ListAccounts;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class ListAccountsQueryHandlerTests
{
    [Fact]
    public async Task Handle_NoAccounts_ReturnsEmpty()
    {
        var repository = new FakeAccountRepository();
        var handler = new ListAccountsQueryHandler(repository);

        var result = await handler.Handle(new ListAccountsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_MultipleAccounts_ReturnsAllOrderedByAccountId()
    {
        var repository = new FakeAccountRepository();
        repository.AddExisting("ACC-002");
        repository.AddExisting("ACC-001");
        var handler = new ListAccountsQueryHandler(repository);

        var result = await handler.Handle(new ListAccountsQuery(), CancellationToken.None);

        Assert.Equal(["ACC-001", "ACC-002"], result.Select(a => a.AccountId));
    }
}
