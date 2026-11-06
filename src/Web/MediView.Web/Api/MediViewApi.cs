using System.Net.Http.Headers;
using MediView.Web.Auth;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace MediView.Web.Api;

public class MediViewApi(HttpClient http, TokenStore tokens)
{
    public const long MaxUploadFileBytes = 64L * 1024 * 1024;

    public async Task<string?> LoginAsync(string email, string password)
    {
        using var response = await SendAsync(HttpMethod.Post, "api/identity/auth/login", new { Email = email, Password = password });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return login?.Token;
    }

    public Task<string?> RegisterAsync(RegisterRequest request) =>
        SendAndReadErrorAsync(HttpMethod.Post, "api/identity/auth/register", request);

    public async Task<List<DoctorSummary>> ListDoctorsAsync() =>
        await GetAsync<List<DoctorSummary>>("api/identity/doctors") ?? [];

    public Task<string?> CreateDoctorAsync(CreateDoctorRequest request) =>
        SendAndReadErrorAsync(HttpMethod.Post, "api/identity/admin/doctors", request);

    public async Task<List<AvailableSlot>> GetAvailabilityAsync(Guid doctorId, DateOnly from, DateOnly to) =>
        await GetAsync<List<AvailableSlot>>($"api/identity/doctors/{doctorId}/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}") ?? [];

    public async Task<List<DateTimeOffset>> GetBookedSlotsAsync(Guid doctorId, DateTimeOffset from, DateTimeOffset to)
    {
        var fromText = Uri.EscapeDataString(from.ToString("O"));
        var toText = Uri.EscapeDataString(to.ToString("O"));
        return await GetAsync<List<DateTimeOffset>>($"api/studies/studies/booked-slots?doctorId={doctorId}&from={fromText}&to={toText}") ?? [];
    }

    public Task<string?> BookAsync(Guid doctorId, DateTimeOffset scheduledStart) =>
        SendAndReadErrorAsync(HttpMethod.Post, "api/studies/appointments", new { DoctorId = doctorId, ScheduledStart = scheduledStart });

    public async Task<List<StudySummary>> ListStudiesAsync() =>
        await GetAsync<List<StudySummary>>("api/studies/studies") ?? [];

    public Task<StudyDetail?> GetStudyAsync(Guid studyId) =>
        GetAsync<StudyDetail>($"api/studies/studies/{studyId}");

    public Task<string?> OpenStudyAsync(Guid studyId) =>
        SendAndReadErrorAsync(HttpMethod.Post, $"api/studies/studies/{studyId}/open");

    public Task<string?> OverrideStudyAsync(Guid studyId, StudyStatus to, string reason) =>
        SendAndReadErrorAsync(HttpMethod.Post, $"api/studies/studies/{studyId}/override", new { To = to, Reason = reason });

    public async Task<List<InstanceSummary>> ListInstancesAsync(Guid studyId) =>
        await GetAsync<List<InstanceSummary>>($"api/imaging/studies/{studyId}/instances") ?? [];

    public async Task<(ImportResult? Result, string? Error)> ImportImagesAsync(Guid studyId, IReadOnlyList<IBrowserFile> files)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            using var memory = new MemoryStream();
            await using (var upload = file.OpenReadStream(MaxUploadFileBytes))
            {
                await upload.CopyToAsync(memory);
            }

            form.Add(new ByteArrayContent(memory.ToArray()), "files", file.Name);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"api/imaging/studies/{studyId}/images") { Content = form };
        using var response = await SendAsync(request);

        var error = await ReadErrorAsync(response);
        if (error is not null)
        {
            return (null, error);
        }

        return (await response.Content.ReadFromJsonAsync<ImportResult>(), null);
    }

    public async Task<List<ReportView>> ListReportsAsync(Guid studyId) =>
        await GetAsync<List<ReportView>>($"api/reporting/reports?studyId={studyId}") ?? [];

    public Task<string?> StartReportAsync(Guid studyId) =>
        SendAndReadErrorAsync(HttpMethod.Post, "api/reporting/reports", new { StudyId = studyId });

    public Task<string?> UpdateReportAsync(Guid reportId, string? findings, string? impression) =>
        SendAndReadErrorAsync(HttpMethod.Put, $"api/reporting/reports/{reportId}", new { Findings = findings, Impression = impression });

    public Task<string?> AddMedicationAsync(Guid reportId, MedicationRequest medication) =>
        SendAndReadErrorAsync(HttpMethod.Post, $"api/reporting/reports/{reportId}/medications", medication);

    public Task<string?> RemoveMedicationAsync(Guid reportId, Guid medicationId) =>
        SendAndReadErrorAsync(HttpMethod.Delete, $"api/reporting/reports/{reportId}/medications/{medicationId}");

    public Task<string?> FinalizeReportAsync(Guid reportId, ReportDecision decision) =>
        SendAndReadErrorAsync(HttpMethod.Post, $"api/reporting/reports/{reportId}/finalize", new { Decision = decision });

    private async Task<T?> GetAsync<T>(string url)
    {
        using var response = await SendAsync(HttpMethod.Get, url);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<string?> SendAndReadErrorAsync(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendAsync(method, url, body);
        return await ReadErrorAsync(response);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return SendAsync(request);
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        var token = await tokens.GetAsync();
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await http.SendAsync(request);
    }

    private static async Task<string?> ReadErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        if (response.Content.Headers.ContentType?.MediaType != "application/problem+json")
        {
            return $"The request failed ({(int)response.StatusCode}).";
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        return problem?.Detail ?? problem?.Title ?? "The request failed.";
    }
}
