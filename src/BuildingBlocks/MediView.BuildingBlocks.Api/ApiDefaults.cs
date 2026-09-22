using MediView.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace MediView.BuildingBlocks.Api;

public static class ApiDefaults
{
    public const string HealthPath = "/health";

    public static WebApplicationBuilder AddApiDefaults(this WebApplicationBuilder builder)
    {
        builder.Host.UseMediViewLogging();

        builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = AddCorrelationId);
        builder.Services.AddExceptionHandler<DomainExceptionHandler>();
        builder.Services.AddHealthChecks();
        builder.Services.AddMediViewAuth(builder.Configuration);

        return builder;
    }

    public static WebApplication UseApiDefaults(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseExceptionHandler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks(HealthPath);

        return app;
    }

    private static void AddCorrelationId(ProblemDetailsContext context)
    {
        if (context.HttpContext.Items[CorrelationIdMiddleware.ItemKey] is string correlationId)
        {
            context.ProblemDetails.Extensions["correlationId"] = correlationId;
        }
    }
}
