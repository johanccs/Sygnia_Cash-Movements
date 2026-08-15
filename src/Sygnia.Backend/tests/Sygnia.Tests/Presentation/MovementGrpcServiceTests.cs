using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sygnia.Application.Interfaces;
using Sygnia.Application.Queries.GetStatementPage;
using Sygnia.Presentation;
using Sygnia.Presentation.Services;
using Sygnia.Tests.Application.Fakes;

namespace Sygnia.Tests.Presentation;

/// <summary>
/// Exercises <see cref="MovementGrpcService"/> against a real <see cref="IMediator"/>, backed by
/// hand-written fakes — same DI-plus-fakes convention as <see cref="AccountGrpcServiceTests"/>.
/// </summary>
public sealed class MovementGrpcServiceTests
{
    private static (MovementGrpcService Service, FakeStatementReader StatementReader) CreateSut()
    {
        var statementReader = new FakeStatementReader();
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetStatementPageQuery>());
        services.AddSingleton<IStatementReader>(statementReader);
        services.AddScoped<IValidator<GetStatementPageQuery>, GetStatementPageQueryValidator>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();
        return (new MovementGrpcService(mediator), statementReader);
    }

    [Fact]
    public async Task GetStatementPage_WithoutFromOrTo_DoesNotThrow()
    {
        var (service, statementReader) = CreateSut();
        statementReader.ExistingAccountIds.Add("ACC-001");

        var response = await service.GetStatementPage(
            new GetStatementPageRequest { AccountId = "ACC-001", PageNumber = 1, PageSize = 20 },
            TestServerCallContext.Create());

        Assert.Equal(0, response.TotalCount);
    }
}
