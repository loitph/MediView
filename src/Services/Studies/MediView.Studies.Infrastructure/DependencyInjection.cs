using MediView.BuildingBlocks.Api.Http;
using MediView.Studies.Application.Booking;
using MediView.Studies.Application.Studies;
using MediView.Studies.Infrastructure.Identity;
using MediView.Studies.Infrastructure.Persistence;
using MediView.Studies.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Studies.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";
    private const string IdentityBaseAddressKey = "Identity:BaseAddress";

    public static IServiceCollection AddStudiesInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<StudiesDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", StudiesDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IStudyRepository, StudyRepository>();
        services.AddScoped<IStudyReadStore>(provider => provider.GetRequiredService<StudiesDbContext>());

        var identityBaseAddress = configuration.GetValue<Uri>(IdentityBaseAddressKey)
            ?? throw new InvalidOperationException($"'{IdentityBaseAddressKey}' is not configured.");

        services.AddHttpClient<IIdentityDirectory, IdentityDirectoryClient>(client => client.BaseAddress = identityBaseAddress)
            .ForwardCallerBearerToken();

        return services;
    }

    public static void MigrateStudiesDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<StudiesDbContext>().Database.Migrate();
    }
}
