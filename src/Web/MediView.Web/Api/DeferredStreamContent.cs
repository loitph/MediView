using System.Net;

namespace MediView.Web.Api;

internal sealed class DeferredStreamContent(UploadFile file) : HttpContent
{
    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
        SerializeToStreamAsync(stream, context, CancellationToken.None);

    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
    {
        await using var content = file.OpenRead();
        await content.CopyToAsync(stream, cancellationToken);
    }

    protected override bool TryComputeLength(out long length)
    {
        length = file.Size;
        return true;
    }
}
