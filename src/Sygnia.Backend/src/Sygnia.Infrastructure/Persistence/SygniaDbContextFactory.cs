using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Sygnia.Infrastructure.Persistence;

/// <summary>
/// Design-time only, so <c>dotnet ef migrations</c> can build <see cref="SygniaDbContext"/>
/// without a running host. The connection string here is never used at runtime — Presentation's
/// composition root passes the real one to <c>AddInfrastructure</c>.
/// </summary>
internal sealed class SygniaDbContextFactory : IDesignTimeDbContextFactory<SygniaDbContext>
{
    // Matches the local docker SQL Server described in this project's CLAUDE.md.
    private const string DesignTimeConnectionString =
        "Server=localhost;Database=sygnia_cash;User Id=sa;Password=@1Mops4moa;TrustServerCertificate=True;";

    public SygniaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SygniaDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new SygniaDbContext(options);
    }
}
