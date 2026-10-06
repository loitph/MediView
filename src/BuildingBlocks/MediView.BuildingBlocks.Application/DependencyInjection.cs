using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace MediView.BuildingBlocks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        services.AddScoped<ISender, Sender>();

        var handlers =
            from type in assembly.GetTypes()
            where type is { IsAbstract: false, IsInterface: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
            select (contract, type);

        foreach (var (contract, type) in handlers)
        {
            services.AddScoped(contract, type);
        }

        return services;
    }
}