using MediView.Reporting.Api;
using MediView.Reporting.Api.Data;
using MediView.Reporting.Api.Endpoints;
using MediView.Shared;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDbContext<ReportingDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "reporting"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<StudiesClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Studies:BaseAddress"]!));

var app = builder.Build();
app.UseServiceDefaults();
app.MapReportEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<ReportingDbContext>().Database.Migrate();
}

app.Run();
