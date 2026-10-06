using Microsoft.AspNetCore.Components.Server.Circuits;

namespace MediView.Web.Auth;

internal sealed class CircuitServicesAccessorHandler(IServiceProvider services, CircuitServicesAccessor accessor)
    : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next) =>
        async context =>
        {
            accessor.Services = services;
            await next(context);
            accessor.Services = null;
        };
}
