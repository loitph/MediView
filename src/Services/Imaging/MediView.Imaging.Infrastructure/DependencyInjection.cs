using MediView.BuildingBlocks.Api.Http;
using MediView.Imaging.Application.Instances;
using MediView.Imaging.Infrastructure.Dicom;
using MediView.Imaging.Infrastructure.Persistence;
using MediView.Imaging.Infrastructure.Persistence.Repositories;
using MediView.Imaging.Infrastructure.Storage;
using MediView.Imaging.Infrastructure.Studies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MediView.Imaging.Infrastructure;

public static class DependencyInjection
{
    private const string ConnectionStringName = "Postgres";
    private const string StorageRootKey = "BlobStorage:Root";
    private const string StudiesBaseAddressKey = "Studies:BaseAddress";

    public static IServiceCollection AddImagingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' is not configured.");

        services.AddDbContext<ImagingDbContext>(options => options
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", ImagingDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IInstanceRepository, InstanceRepository>();
        services.AddSingleton<IDicomReader, FoDicomReader>();

        var storageRoot = configuration[StorageRootKey]
            ?? throw new InvalidOperationException($"'{StorageRootKey}' is not configured.");
        services.AddSingleton<IBlobStore>(provider => new LocalDiskBlobStore(
            Path.GetFullPath(storageRoot, provider.GetRequiredService<IHostEnvironment>().ContentRootPath)));

        var studiesBaseAddress = configuration.GetValue<Uri>(StudiesBaseAddressKey)
            ?? throw new InvalidOperationException($"'{StudiesBaseAddressKey}' is not configured.");
        services.AddHttpClient<IStudiesClient, StudiesClient>(client => client.BaseAddress = studiesBaseAddress)
            .ForwardCallerBearerToken();

        return services;
    }

    public static void MigrateImagingDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ImagingDbContext>().Database.Migrate();
    }
}
