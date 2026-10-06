using System.Net;
using MediView.Imaging.Application.Instances;

namespace MediView.Imaging.Infrastructure.Studies;

internal sealed class StudiesClient(HttpClient http) : IStudiesClient
{
    public async Task<bool> CanReadAsync(Guid studyId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"studies/{studyId}", UriKind.Relative), cancellationToken);
        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<bool> TryMarkImagesImportedAsync(Guid studyId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsync(
                new Uri($"internal/studies/{studyId}/images-imported", UriKind.Relative),
                content: null,
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
