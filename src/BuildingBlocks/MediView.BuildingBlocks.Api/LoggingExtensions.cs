using System.Globalization;
using System.Reflection;
using MediView.BuildingBlocks.Api.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace MediView.BuildingBlocks.Api;

/// <summary>
/// The one logging setup all four services share: structured events to the console for the
/// developer and to Seq for searching, every line tagged with ServiceName, CorrelationId and
/// (once authenticated) UserId.
/// </summary>
public static class LoggingExtensions
{
    private const string DefaultSeqServerUrl = "http://localhost:5341";

    private const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {CorrelationId} {Message:lj}{NewLine}{Exception}";

    /// <summary>Replaces the default logging providers with Serilog. Call it on builder.Host.</summary>
    public static IHostBuilder UseMediViewLogging(this IHostBuilder host)
    {
        // the UserId enricher reads the current principal, so the accessor has to be in the container
        host.ConfigureServices(services => services.AddHttpContextAccessor());

        return host.UseSerilog((ctx, services, cfg) =>
        {
            var serviceName = Assembly.GetEntryAssembly()?.GetName().Name ?? "MediView.Unknown";

            cfg.ReadFrom.Configuration(ctx.Configuration)   // levels and overrides live in appsettings
               .Enrich.FromLogContext()                     // CorrelationId rides in on the LogContext
               .Enrich.WithProperty("ServiceName", serviceName)
               .Enrich.WithMachineName()
               .Enrich.WithEnvironmentName()
               .Enrich.With(new UserIdEnricher(services.GetRequiredService<IHttpContextAccessor>()))
               .WriteTo.Console(outputTemplate: ConsoleOutputTemplate, formatProvider: CultureInfo.InvariantCulture)
               .WriteTo.Seq(
                   ctx.Configuration["Seq:ServerUrl"] ?? DefaultSeqServerUrl,
                   formatProvider: CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Correlation id first, then one summary line per HTTP request. Order matters: the summary
    /// line is written after the pipeline unwinds, so the correlation id must still be on the
    /// LogContext at that point.
    /// </summary>
    public static WebApplication UseMediViewRequestLogging(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        return app;
    }
}
