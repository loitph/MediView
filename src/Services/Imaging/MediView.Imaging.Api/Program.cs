using MediView.BuildingBlocks.Api;
using MediView.Imaging.Api.Instances;
using MediView.Imaging.Application;
using MediView.Imaging.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = InstanceEndpoints.MaxUploadBytes);
builder.AddApiDefaults();
builder.Services.AddImagingApplication();
builder.Services.AddImagingInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseApiDefaults();
app.MapInstanceEndpoints();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateImagingDatabase();
}

app.Run();
