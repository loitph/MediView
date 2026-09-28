using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using MediView.Studies.Infrastructure.Locking;
using MediView.Studies.Infrastructure.Persistence;
using MediView.Studies.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace MediView.Studies.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";
    private const string RedisConfigurationKey = "Redis:Configuration";

    public static IServiceCollection AddStudiesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<StudiesDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", StudiesDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IStudyRepository, StudyRepository>();

        var redisConfiguration = configuration[RedisConfigurationKey]
            ?? throw new InvalidOperationException($"'{RedisConfigurationKey}' is not configured.");

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfiguration));
        services.AddSingleton<IStudyLock, RedisStudyLock>();

        return services;
    }

    public static void MigrateStudiesDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<StudiesDbContext>().Database.Migrate();
    }
}
