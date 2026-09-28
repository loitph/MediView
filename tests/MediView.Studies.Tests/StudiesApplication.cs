using MediView.BuildingBlocks.Application;
using MediView.Studies.Application;
using MediView.Studies.Application.Locking;
using MediView.Studies.Application.Studies;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.Studies.Tests;

internal sealed class StudiesApplication(IStudyLock studyLock, IStudyRepository studies) : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddStudiesApplication()
        .AddSingleton(studyLock)
        .AddSingleton(studies)
        .AddSingleton(TimeProvider.System)
        .BuildServiceProvider();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(request, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
