using MediView.BuildingBlocks.Api;
using MediView.Identity.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseMediViewLogging();   // console + Seq, enriched with ServiceName/CorrelationId/UserId
builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();   // missing secret => fails at startup, not at runtime
var app = builder.Build();
app.UseMediViewRequestLogging();   // correlation id, then one summary line per request

app.MapGet("/", () => "Hello World!");

app.Run();
