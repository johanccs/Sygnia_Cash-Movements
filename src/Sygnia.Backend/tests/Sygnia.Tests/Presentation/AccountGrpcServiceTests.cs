using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Core;
using Sygnia.Application.Commands.CreateAccount;
using Sygnia.Application.Interfaces;
using Sygnia.Presentation;
using Sygnia.Presentation.Services;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Presentation;

/// <summary>
/// Exercises <see cref="AccountGrpcService"/> against a real <see cref="IMediator"/> wired to
/// the actual command/query handlers, backed by <see cref="FakeAccountRepository"/> instead of
/// a database — this codebase has no mocking library, so DI + hand-written fakes stand in for
/// it, same as the Application-layer handler tests.
/// </summary>
public sealed class AccountGrpcServiceTests
{
    private static (AccountGrpcService Service, FakeAccountRepository Repository) CreateSut()
    {
        var repository = new FakeAccountRepository();
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateAccountCommand>());
        services.AddSingleton<IAccountRepository>(repository);
        services.AddScoped<IValidator<CreateAccountCommand>, CreateAccountCommandValidator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        return (new AccountGrpcService(mediator), repository);
    }

    [Fact]
    public async Task CreateAccount_Valid_ReturnsAccountProto()
    {
        var (service, _) = CreateSut();

        var response = await service.CreateAccount(
            new CreateAccountRequest { AccountId = "ACC-001", AccountName = "Test", Currency = "ZAR", CreatedBy = "seed" },
            TestServerCallContext.Create());

        Assert.Equal("ACC-001", response.AccountId);
    }

    [Fact]
    public async Task CreateAccount_DuplicateId_ThrowsAlreadyExists()
    {
        var (service, repository) = CreateSut();
        repository.AddExisting("ACC-001");

        var ex = await Assert.ThrowsAsync<RpcException>(() => service.CreateAccount(
            new CreateAccountRequest { AccountId = "ACC-001", AccountName = "Test", Currency = "ZAR", CreatedBy = "seed" },
            TestServerCallContext.Create()));

        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }

    [Fact]
    public async Task GetAccount_Existing_ReturnsAccountProto()
    {
        var (service, repository) = CreateSut();
        repository.AddExisting("ACC-001");

        var response = await service.GetAccount(
            new GetAccountRequest { AccountId = "ACC-001" },
            TestServerCallContext.Create());

        Assert.Equal("ACC-001", response.AccountId);
    }

    [Fact]
    public async Task GetAccount_Unknown_ThrowsNotFound()
    {
        var (service, _) = CreateSut();

        var ex = await Assert.ThrowsAsync<RpcException>(() => service.GetAccount(
            new GetAccountRequest { AccountId = "ACC-999" },
            TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
