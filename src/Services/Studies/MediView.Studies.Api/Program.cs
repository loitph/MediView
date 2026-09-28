using MediView.BuildingBlocks.Api;
using MediView.Studies.Api.Studies;
using MediView.Studies.Application;
using MediView.Studies.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddStudiesApplication();
builder.Services.AddStudiesInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseApiDefaults();
app.MapStudyEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateStudiesDatabase();
}

app.Run();
