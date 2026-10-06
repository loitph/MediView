using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MediView.BuildingBlocks.Api.Http;

public static class BearerTokenForwarding
{
    public static IHttpClientBuilder ForwardCallerBearerToken(this IHttpClientBuilder builder)
    {
        builder.Services.AddHttpContextAccessor();
        builder.Services.TryAddTransient<ForwardedBearerTokenHandler>();
        return builder.AddHttpMessageHandler<ForwardedBearerTokenHandler>();
    }

    private sealed class ForwardedBearerTokenHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var authorization = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (AuthenticationHeaderValue.TryParse(authorization, out var header))
            {
                request.Headers.Authorization = header;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
