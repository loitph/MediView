using MediView.BuildingBlocks.Application;
using MediView.Studies.Application;
using MediView.Studies.Application.Booking;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.Studies.Tests;

internal sealed class StudiesApplication : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    public StudiesApplication(IStudyLock studyLock, IStudyRepository studies)
        : this(services => services.AddSingleton(studyLock).AddSingleton(studies))
    {
    }

    public StudiesApplication(IIdentityDirectory identity, IStudyRepository studies)
        : this(services => services.AddSingleton(identity).AddSingleton(studies))
    {
    }

    private StudiesApplication(Action<IServiceCollection> register)
    {
        var services = new ServiceCollection()
            .AddStudiesApplication()
            .AddSingleton(TimeProvider.System);
        register(services);
        _services = services.BuildServiceProvider();
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(request, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
