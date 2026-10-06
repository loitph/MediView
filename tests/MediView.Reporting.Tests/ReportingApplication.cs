using MediView.BuildingBlocks.Application;
using MediView.Reporting.Application;
using MediView.Reporting.Application.Reports;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.Reporting.Tests;

internal sealed class ReportingApplication(IStudiesClient studies, IReportRepository reports) : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddReportingApplication()
        .AddSingleton(studies)
        .AddSingleton(reports)
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
