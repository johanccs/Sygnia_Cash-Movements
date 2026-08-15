using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sygnia.Application.Interfaces;
using Sygnia.Infrastructure.Persistence;
using Sygnia.Infrastructure.Repositories;

namespace Sygnia.Infrastructure;

/// <summary>
/// This layer's one composition-root entry point. Repositories are internal sealed, so
/// registration can only happen here.
/// </summary>
public static class InfrastructureModuleExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<SygniaDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBalanceReader, BalanceReader>();
        services.AddScoped<IStatementReader, StatementReader>();

        return services;
    }
}
