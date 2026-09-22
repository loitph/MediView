using MediView.BuildingBlocks.Api;
using MediView.Imaging.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();
builder.Services.AddImagingInfrastructure(builder.Configuration);

var app = builder.Build();
app.UseApiDefaults();

if (app.Environment.IsDevelopment())
{
    app.Services.MigrateImagingDatabase();
}

app.Run();
