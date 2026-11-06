using MediView.Imaging.Api;
using MediView.Imaging.Api.Data;
using MediView.Imaging.Api.Endpoints;
using MediView.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = InstanceEndpoints.MaxUploadBytes);
builder.AddServiceDefaults();
builder.Services.AddDbContext<ImagingDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "imaging"))
    .UseSnakeCaseNamingConvention());

var imagesFolder = Path.GetFullPath(builder.Configuration["BlobStorage:Root"]!, builder.Environment.ContentRootPath);
builder.Services.AddSingleton(new ImageStorage(imagesFolder));

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<StudiesClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Studies:BaseAddress"]!));

var app = builder.Build();
app.UseServiceDefaults();
app.MapInstanceEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ImagingDbContext>().Database.Migrate();
}

app.Run();
