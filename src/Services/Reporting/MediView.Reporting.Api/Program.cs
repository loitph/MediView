using MediView.BuildingBlocks.Api;
using MediView.Reporting.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddReportingInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseApiDefaults();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateReportingDatabase();
}

app.Run();
