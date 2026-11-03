using System.Net;
using System.Net.Http.Json;
using MediView.Reporting.Application.Reports;
using MediView.Reporting.Domain.Reports;

namespace MediView.Reporting.Infrastructure.Studies;

internal sealed class StudiesClient(HttpClient http) : IStudiesClient
{
    private const string InProgress = "InProgress";

    public async Task<Guid?> FindReaderAsync(Guid studyId, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(new Uri($"studies/{studyId}", UriKind.Relative), cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var detail = await response.Content.ReadFromJsonAsync<StudyDetail>(cancellationToken);
        return detail?.Study is { Status: InProgress } study ? study.DoctorId : null;
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

    private sealed record StudyDetail(StudySnapshot Study);

    private sealed record StudySnapshot(Guid DoctorId, string Status);

    private sealed record FinishReadRequest(string Decision);
}
