using MediView.Identity.Api;
using MediView.Identity.Api.Data;
using MediView.Identity.Api.Endpoints;
using MediView.Identity.Api.Entities;
using MediView.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDbContext<IdentityDbContext>(options => options
    .UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "identity"))
    .UseSnakeCaseNamingConvention());
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();

var app = builder.Build();
app.UseServiceDefaults();
app.MapAuthEndpoints();
app.MapDoctorEndpoints();
app.MapPatientEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
    SeedData.AddDemoAccounts(db, app.Services.GetRequiredService<PasswordHasher<User>>());
}

app.Run();
