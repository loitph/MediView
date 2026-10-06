using System.Net;
using System.Net.Http.Json;
using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Infrastructure.Studies;

internal sealed class StudiesClient(HttpClient http) : IStudiesClient
{
    public async Task<Guid?> FindLockOwnerAsync(Guid studyId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"studies/{studyId}/lock", UriKind.Relative), cancellationToken);
        response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            return null;
        }

        var owner = await response.Content.ReadFromJsonAsync<LockOwner>(cancellationToken);
        return owner?.DoctorId;
    }

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

    public async Task<bool> TryFinishReadAsync(Guid studyId, ReportDecision decision, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await http.PostAsJsonAsync(
                new Uri($"internal/studies/{studyId}/finish-read", UriKind.Relative),
                new FinishReadRequest(decision.ToString()),
                cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private sealed record LockOwner(Guid DoctorId, string DoctorName);

    private sealed record FinishReadRequest(string Decision);
}
