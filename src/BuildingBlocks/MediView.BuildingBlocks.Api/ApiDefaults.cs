using System.Globalization;
using MediView.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MediView.BuildingBlocks.Api;

public static class ApiDefaults
{
    private const string ConsoleOutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] {ServiceName} {Message:lj}{NewLine}{Exception}";

    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;
        builder.Host.UseSerilog((context, logging) => logging
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.WithProperty("ServiceName", serviceName)
            .WriteTo.Console(outputTemplate: ConsoleOutputTemplate, formatProvider: CultureInfo.InvariantCulture));

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<DomainExceptionHandler>();
        builder.Services.AddMediViewAuth(builder.Configuration);

        return builder;
    }

    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}
