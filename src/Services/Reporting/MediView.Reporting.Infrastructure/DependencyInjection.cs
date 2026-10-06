using MediView.BuildingBlocks.Api.Http;
using MediView.Reporting.Application.Reports;
using MediView.Reporting.Infrastructure.Persistence;
using MediView.Reporting.Infrastructure.Persistence.Repositories;
using MediView.Reporting.Infrastructure.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Reporting.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";
    private const string StudiesBaseAddressKey = "Studies:BaseAddress";

    public static IServiceCollection AddReportingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ReportingDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ReportingDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IReportRepository, ReportRepository>();

        var studiesBaseAddress = configuration.GetValue<Uri>(StudiesBaseAddressKey)
            ?? throw new InvalidOperationException($"'{StudiesBaseAddressKey}' is not configured.");
        services.AddHttpClient<IStudiesClient, StudiesClient>(client => client.BaseAddress = studiesBaseAddress)
            .ForwardCallerBearerToken();

        return services;
    }

    public static void MigrateReportingDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ReportingDbContext>().Database.Migrate();
    }
}
