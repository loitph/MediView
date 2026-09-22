using MediView.Web.Api;
using MediView.Web.Auth;
using MediView.Web.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<CircuitServicesAccessor>();
builder.Services.AddScoped<CircuitHandler, CircuitServicesAccessorHandler>();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    services => services.GetRequiredService<JwtAuthenticationStateProvider>());

var gatewayAddress = builder.Configuration.GetValue<Uri>("Gateway:BaseAddress")
    ?? throw new InvalidOperationException("Gateway:BaseAddress is not configured.");

builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<MediViewApi>(client => client.BaseAddress = gatewayAddress)
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
