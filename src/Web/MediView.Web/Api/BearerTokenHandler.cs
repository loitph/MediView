using System.Net.Http.Headers;
using MediView.Web.Auth;

namespace MediView.Web.Api;

internal sealed class BearerTokenHandler(CircuitServicesAccessor circuit) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var tokens = circuit.Services?.GetService<TokenStore>();
        if (tokens is not null && await tokens.GetAsync() is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
