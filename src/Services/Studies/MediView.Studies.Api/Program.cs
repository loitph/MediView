using System.Text.Json.Serialization;
using MediView.BuildingBlocks.Api;
using MediView.Studies.Api.Booking;
using MediView.Studies.Api.Studies;
using MediView.Studies.Application;
using MediView.Studies.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddStudiesApplication();
builder.Services.AddStudiesInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseApiDefaults();
app.MapAppointmentEndpoints();
app.MapStudyEndpoints();
app.MapStudyLockEndpoints();
app.MapInternalStudyEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateStudiesDatabase();
}

app.Run();
