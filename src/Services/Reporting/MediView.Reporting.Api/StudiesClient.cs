using MediView.Reporting.Api.Entities;

namespace MediView.Reporting.Api;

public record StudyInfo(Guid DoctorId, string Status);

public class StudiesClient
{
    private readonly HttpClient http;

    public StudiesClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        this.http = http;
        var callerToken = httpContextAccessor.HttpContext!.Request.Headers.Authorization.ToString();
        http.DefaultRequestHeaders.Add("Authorization", callerToken);
    }

    public async Task<StudyInfo?> GetStudyAsync(Guid studyId)
    {
        using var response = await http.GetAsync($"studies/{studyId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var detail = await response.Content.ReadFromJsonAsync<StudyDetail>();
        return detail?.Study;
    }

    public async Task<bool> FinishReadAsync(Guid studyId, ReportDecision decision)
    {
        var body = new { Decision = decision.ToString() };
        using var response = await http.PostAsJsonAsync($"internal/studies/{studyId}/finish-read", body);
        return response.IsSuccessStatusCode;
    }

    private record StudyDetail(StudyInfo Study);
}
