using MediView.Web.Api;
using MediView.Web.Auth;
using MediView.Web.Components;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<TokenStore>();
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(services =>
    services.GetRequiredService<JwtAuthenticationStateProvider>());

var gatewayAddress = builder.Configuration["Gateway:BaseAddress"]!;
builder.Services.AddHttpClient<MediViewApi>(client => client.BaseAddress = new Uri(gatewayAddress));
builder.Services.AddHttpForwarder();

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
app.MapForwarder("/api/{**catch-all}", gatewayAddress);
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
