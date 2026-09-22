using MediView.BuildingBlocks.Api;
using MediView.Studies.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddStudiesInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseApiDefaults();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateStudiesDatabase();
}

app.Run();
