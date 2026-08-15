using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Sygnia.Application.Behaviours;
using Sygnia.Application.Commands.SubmitMovement;
using Sygnia.Application.Commands.TransferFunds;
using Sygnia.Application.Queries.GetBalance;
using Sygnia.Application.Queries.GetStatement;

namespace Sygnia.Application;

/// <summary>
/// This layer's one composition-root entry point. Everything else in the project is
/// <c>internal sealed</c>, so registration can only happen here.
/// <para>
/// The originating instruction named this a <c>Register</c> extension on
/// <c>IServiceProvider</c>; DI registration has to run against <c>IServiceCollection</c> (the
/// container being built, not the built provider), so it is implemented as that instead —
/// the naming (<c>AppModuleExtensions.Register</c>) is kept as specified.
/// </para>
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

        // TODO: IMovementRepository, IBalanceReader, IStatementReader are registered by
        // Sygnia.Infrastructure's AddInfrastructure(connectionString), against the EF Core
        // DbContext, once that project exists.

        return services;
    }
}
