using Microsoft.Extensions.DependencyInjection;

namespace MediView.BuildingBlocks.Application;

public interface ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

internal sealed class Sender(IServiceProvider serviceProvider) : ISender
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Read the request type
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));

        // Resolve the handler
        var handler = serviceProvider.GetRequiredService(handlerType);

        // Return response from the handler
        var handle = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;
        return (Task<TResponse>)handle.Invoke(handler, [request, cancellationToken])!;
    }
}