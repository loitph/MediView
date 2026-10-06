using MediView.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Imaging.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddImagingApplication(this IServiceCollection services) =>
        services.AddApplication(typeof(DependencyInjection).Assembly);
}
