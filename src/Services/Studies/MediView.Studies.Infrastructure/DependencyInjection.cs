using MediView.Studies.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Studies.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddStudiesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<StudiesDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", StudiesDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    public static void MigrateStudiesDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<StudiesDbContext>().Database.Migrate();
    }
}
