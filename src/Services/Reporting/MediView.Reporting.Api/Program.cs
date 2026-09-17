using MediView.BuildingBlocks.Api;
using MediView.Reporting.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseMediViewLogging();
builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
var app = builder.Build();
app.UseMediViewRequestLogging();

app.MapGet("/", () => "Hello World!");

app.Run();
