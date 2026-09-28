using MediView.BuildingBlocks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.Studies.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddStudiesApplication(this IServiceCollection services) =>
        services.AddApplication(typeof(DependencyInjection).Assembly);
}
