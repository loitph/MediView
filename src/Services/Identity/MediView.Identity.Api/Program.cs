using MediView.BuildingBlocks.Api;
using MediView.Identity.Api.Auth;
using MediView.Identity.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

var app = builder.Build();
app.UseApiDefaults();
app.MapAuthEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateIdentityDatabase();
}

app.Run();
