using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MediView.BuildingBlocks.Api.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddMediViewAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(
                jwt => jwt.HasStrongSigningKey,
                $"{JwtOptions.SectionName}:SigningKey must be at least {JwtOptions.MinimumSigningKeyBytes} bytes.")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) => ConfigureBearer(bearer, jwt.Value));

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.PatientOnly, policy => policy.RequireRole(MediViewRoles.Patient))
            .AddPolicy(AuthPolicies.DoctorOnly, policy => policy
                .RequireRole(MediViewRoles.Doctor)
                .RequireClaim(MediViewClaims.DoctorId))
            .AddPolicy(AuthPolicies.AdminOnly, policy => policy.RequireRole(MediViewRoles.Admin));

        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, JwtOptions jwt)
    {
        bearer.MapInboundClaims = false;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = jwt.CreateSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = MediViewClaims.Name,
            RoleClaimType = MediViewClaims.Role,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
