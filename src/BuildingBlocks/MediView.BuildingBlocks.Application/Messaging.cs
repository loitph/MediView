namespace MediView.BuildingBlocks.Application;

// Covariance response
public interface IRequest<out TResponse>;

// Ask to Change
public interface ICommand<out TResponse> : IRequest<TResponse>;

// Ask to Read
public interface IQuery<out TResponse> : IRequest<TResponse>;

// Make sure the Requester is a Command or Query
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse> where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse> where  TQuery : IQuery<TResponse>;