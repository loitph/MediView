using System.Text.Json.Serialization;
using MediView.BuildingBlocks.Api;
using MediView.Identity.Api.Auth;
using MediView.Identity.Api.Doctors;
using MediView.Identity.Api.Internal;
using MediView.Identity.Application;
using MediView.Identity.Application.Auth;
using MediView.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

var app = builder.Build();
app.UseApiDefaults();
app.MapAuthEndpoints();
app.MapDoctorEndpoints();
app.MapInternalEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateIdentityDatabase();
    app.Services.SeedIdentityDatabase();
}

app.Run();
