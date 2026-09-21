using MediView.BuildingBlocks.Api;
using MediView.Identity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddIdentityInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseApiDefaults();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateIdentityDatabase();
}

app.Run();
