using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sygnia.Application.Behaviours;
using Sygnia.Application.Commands.CreateAccount;
using Sygnia.Application.Commands.CreateUser;
using Sygnia.Application.Commands.SubmitMovement;
using Sygnia.Application.Commands.TransferFunds;
using Sygnia.Application.Queries.GetBalance;
using Sygnia.Application.Queries.GetStatement;
using Sygnia.Application.Queries.GetStatementPage;

namespace Sygnia.Application;

/// <summary>
/// This layer's one composition-root entry point. Everything else in the project is
/// <c>internal sealed</c>, so registration can only happen here. Registers against
/// <c>IServiceCollection</c> — the container being built, not the built <c>IServiceProvider</c>.
/// </summary>
public static class AppModuleExtensions
{
    public static IServiceCollection Register(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));

        services.AddScoped<IValidator<SubmitMovementCommand>, SubmitMovementCommandValidator>();
        services.AddScoped<IValidator<TransferFundsCommand>, TransferFundsCommandValidator>();
        services.AddScoped<IValidator<GetBalanceQuery>, GetBalanceQueryValidator>();
        services.AddScoped<IValidator<GetStatementQuery>, GetStatementQueryValidator>();
        services.AddScoped<IValidator<GetStatementPageQuery>, GetStatementPageQueryValidator>();
        services.AddScoped<IValidator<CreateAccountCommand>, CreateAccountCommandValidator>();
        services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();

        // IMovementRepository, IAccountRepository, IUserRepository, IBalanceReader,
        // IStatementReader are registered by Sygnia.Infrastructure's
        // AddInfrastructure(connectionString), against the EF Core DbContext.

        return services;
    }
}
