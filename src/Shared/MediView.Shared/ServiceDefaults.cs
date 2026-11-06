using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace MediView.Shared;

public static class ServiceDefaults
{
    public const string JwtIssuer = "mediview-identity";
    public const string JwtAudience = "mediview";

    public static void AddServiceDefaults(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Environment.ApplicationName;
        builder.Host.UseSerilog((context, logger) => logger
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] " + serviceName + " {Message:lj}{NewLine}{Exception}"));

        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = JwtIssuer,
                    ValidAudience = JwtAudience,
                    IssuerSigningKey = GetSigningKey(builder.Configuration),
                    NameClaimType = UserClaims.Name,
                    RoleClaimType = UserClaims.Role,
                };
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(Roles.Patient, policy => policy.RequireRole(Roles.Patient))
            .AddPolicy(Roles.Doctor, policy => policy.RequireRole(Roles.Doctor))
            .AddPolicy(Roles.Admin, policy => policy.RequireRole(Roles.Admin));
    }

    public static void UseServiceDefaults(this WebApplication app)
    {
        app.UseSerilogRequestLogging();
        app.Use(TurnDomainExceptionsIntoBadRequests);
        app.UseAuthentication();
        app.UseAuthorization();
    }

    public static SymmetricSecurityKey GetSigningKey(IConfiguration configuration) =>
        new(Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!));

    private static async Task TurnDomainExceptionsIntoBadRequests(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            await Results.Problem(exception.Message, statusCode: StatusCodes.Status400BadRequest).ExecuteAsync(context);
        }
    }
}
