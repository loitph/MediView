using MediView.Identity.Infrastructure.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();   // missing secret => fails at startup, not at runtime
var app = builder.Build();
app.MapGet("/", () => "Hello World!");

app.Run();
