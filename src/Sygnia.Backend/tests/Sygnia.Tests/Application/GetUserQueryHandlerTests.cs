using Sygnia.Application.Queries.GetUser;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Application;

public sealed class GetUserQueryHandlerTests
{
    private const string UserId = "USR-001";

    [Fact]
    public async Task Handle_ExistingUser_ReturnsSuccess()
    {
        var repository = new FakeUserRepository();
        repository.AddExisting(UserId);
        var handler = new GetUserQueryHandler(repository);

        var result = await handler.Handle(new GetUserQuery(UserId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(UserId, result.Value.Id);
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFoundFailure()
    {
        var repository = new FakeUserRepository();
        var handler = new GetUserQueryHandler(repository);

        var result = await handler.Handle(new GetUserQuery(UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("user.not_found", result.Error.Code);
    }
}
