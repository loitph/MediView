using MediView.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services) =>
        services.AddApplication(typeof(DependencyInjection).Assembly);
}
