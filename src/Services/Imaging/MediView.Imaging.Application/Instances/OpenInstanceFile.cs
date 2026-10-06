using MediView.BuildingBlocks.Application;

namespace MediView.Imaging.Application.Instances;

public sealed record OpenInstanceFileQuery(Guid InstanceId) : IQuery<Stream?>;

internal sealed class OpenInstanceFileQueryHandler(IInstanceRepository instances, IBlobStore blobs)
    : IQueryHandler<OpenInstanceFileQuery, Stream?>
{
    public async Task<Stream?> Handle(OpenInstanceFileQuery request, CancellationToken cancellationToken) =>
        await instances.FindStoragePathAsync(request.InstanceId, cancellationToken) is { } key
            ? blobs.OpenRead(key)
            : null;
}
