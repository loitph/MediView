using MediView.Shared;
using MediView.Studies.Api;
using MediView.Studies.Api.Data;
using MediView.Studies.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDbContext<StudiesDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "studies"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IdentityClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Identity:BaseAddress"]!));

var app = builder.Build();
app.UseServiceDefaults();
app.MapAppointmentEndpoints();
app.MapStudyEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<StudiesDbContext>().Database.Migrate();
}

app.Run();
