using System.Text.Json.Serialization;
using MediView.BuildingBlocks.Api;
using MediView.Reporting.Api.Reports;
using MediView.Reporting.Application;
using MediView.Reporting.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddReportingApplication();
builder.Services.AddReportingInfrastructure(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseApiDefaults();
app.MapReportEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateReportingDatabase();
}

app.Run();
