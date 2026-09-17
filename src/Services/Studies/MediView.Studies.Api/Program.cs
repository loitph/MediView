using MediView.BuildingBlocks.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseMediViewLogging();

var app = builder.Build();
app.UseMediViewRequestLogging();

app.Run();
