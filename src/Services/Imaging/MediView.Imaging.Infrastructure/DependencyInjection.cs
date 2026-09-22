using MediView.Imaging.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Imaging.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddImagingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ImagingDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ImagingDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    public static void MigrateImagingDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ImagingDbContext>().Database.Migrate();
    }
}
