using MediView.BuildingBlocks.Application;
using MediView.Imaging.Application;
using MediView.Imaging.Application.Instances;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MediView.Imaging.Tests;

internal sealed class ImagingApplication(
    IStudiesClient studies,
    IDicomReader dicom,
    IBlobStore blobs,
    IInstanceRepository instances) : IAsyncDisposable
{
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddImagingApplication()
        .AddSingleton(studies)
        .AddSingleton(dicom)
        .AddSingleton(blobs)
        .AddSingleton(instances)
        .BuildServiceProvider();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
    {
        await using var scope = _services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(request, TestContext.Current.CancellationToken);
    }

    public ValueTask DisposeAsync() => _services.DisposeAsync();
}
