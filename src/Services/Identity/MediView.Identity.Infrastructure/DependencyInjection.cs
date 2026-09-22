using MediView.Identity.Application.Auth;
using MediView.Identity.Domain.Users;
using MediView.Identity.Infrastructure.Persistence;
using MediView.Identity.Infrastructure.Persistence.Repositories;
using MediView.Identity.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Identity.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<IdentityDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", IdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }

    public static void MigrateIdentityDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
    }

    public static void SeedIdentityDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentitySeeder>().Seed();
    }
}
