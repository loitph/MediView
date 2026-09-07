using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace MediView.BuildingBlocks.Api;

/// <summary>
/// Gives every request one id that follows it through the logs: taken from the inbound
/// X-Correlation-Id header when a caller supplies one, minted otherwise, echoed back on the
/// response, and pushed into the Serilog LogContext so every line of the request carries it.
/// Register it before UseSerilogRequestLogging so the request summary line is tagged too.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task Invoke(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(HeaderName, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.Response.Headers[HeaderName] = id;

        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(ctx);
        }
    }
}
