using System.Globalization;
using System.Reflection;
using MediView.BuildingBlocks.Api.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MediView.BuildingBlocks.Api;

public static class LoggingExtensions
{
    private const string DefaultSeqServerUrl = "http://localhost:5341";

    private const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {CorrelationId} {Message:lj}{NewLine}{Exception}";

    public static IHostBuilder UseMediViewLogging(this IHostBuilder host)
    {
        host.ConfigureServices(services => services.AddHttpContextAccessor());

        return host.UseSerilog((ctx, services, cfg) =>
        {
            var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "MediView.Unknown";

            cfg.ReadFrom.Configuration(ctx.Configuration)
               .Enrich.FromLogContext()
               .Enrich.WithProperty("ServiceName", serviceName)
               .Enrich.With(new UserIdEnricher(services.GetRequiredService<IHttpContextAccessor>()))
               .WriteTo.Console(outputTemplate: ConsoleOutputTemplate, formatProvider: CultureInfo.InvariantCulture)
               .WriteTo.Seq(
                   ctx.Configuration["Seq:ServerUrl"] ?? DefaultSeqServerUrl,
                   formatProvider: CultureInfo.InvariantCulture);
        });
    }
}
