using MediView.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddApiDefaults();

var app = builder.Build();
app.UseApiDefaults();

app.Run();
