using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Core;
using Sygnia.Application.Commands.CreateUser;
using Sygnia.Application.Interfaces;
using Sygnia.Presentation;
using Sygnia.Presentation.Services;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Presentation;

/// <summary>
/// Exercises <see cref="UserGrpcService"/> against a real <see cref="IMediator"/> wired to
/// the actual command/query handlers, backed by <see cref="FakeUserRepository"/> instead of
/// a database — this codebase has no mocking library, so DI + hand-written fakes stand in for
/// it, same as the Application-layer handler tests.
/// </summary>
public sealed class UserGrpcServiceTests
{
    private static (UserGrpcService Service, FakeUserRepository Repository) CreateSut()
    {
        var repository = new FakeUserRepository();
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CreateUserCommand>());
        services.AddSingleton<IUserRepository>(repository);
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        return (new UserGrpcService(mediator), repository);
    }

    [Fact]
    public async Task CreateUser_Valid_ReturnsUserProto()
    {
        var (service, _) = CreateSut();

        var response = await service.CreateUser(
            new CreateUserRequest { Id = "USR-001", Name = "Jane", Surname = "Doe" },
            TestServerCallContext.Create());

        Assert.Equal("USR-001", response.Id);
    }

    [Fact]
    public async Task CreateUser_DuplicateId_ThrowsAlreadyExists()
    {
        var (service, repository) = CreateSut();
        repository.AddExisting("USR-001");

        var ex = await Assert.ThrowsAsync<RpcException>(() => service.CreateUser(
            new CreateUserRequest { Id = "USR-001", Name = "Jane", Surname = "Doe" },
            TestServerCallContext.Create()));

        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }

    [Fact]
    public async Task GetUser_Existing_ReturnsUserProto()
    {
        var (service, repository) = CreateSut();
        repository.AddExisting("USR-001");

        var response = await service.GetUser(
            new GetUserRequest { Id = "USR-001" },
            TestServerCallContext.Create());

        Assert.Equal("USR-001", response.Id);
    }

    [Fact]
    public async Task GetUser_Unknown_ThrowsNotFound()
    {
        var (service, _) = CreateSut();

        var ex = await Assert.ThrowsAsync<RpcException>(() => service.GetUser(
            new GetUserRequest { Id = "USR-999" },
            TestServerCallContext.Create()));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
