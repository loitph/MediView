using System.Globalization;
using System.Net;
using System.Net.Http.Headers;

namespace MediView.Web.Api;

public sealed class MediViewApi(HttpClient http)
{
    private const string DicomMediaType = "application/dicom";

    public async Task<AccessToken?> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync(
            "api/identity/auth/login",
            new LoginRequest(email, password),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AccessToken>(cancellationToken);
    }

    public async Task<ApiOutcome> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/identity/auth/register", request, cancellationToken);
        return await ApiOutcome.FromAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<DoctorSummary>> ListDoctorsAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<DoctorSummary>>("api/identity/doctors", cancellationToken) ?? [];

    public async Task<ApiOutcome> CreateDoctorAsync(CreateDoctorRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/identity/admin/doctors", request, cancellationToken);
        return await ApiOutcome.FromAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<AvailableSlot>> GetAvailabilityAsync(
        Guid doctorId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<AvailableSlot>>(
            string.Create(
                CultureInfo.InvariantCulture,
                $"api/identity/doctors/{doctorId}/availability?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"),
            cancellationToken) ?? [];

    public async Task<IReadOnlyList<DateTimeOffset>> GetBookedSlotsAsync(
        Guid doctorId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<DateTimeOffset>>(
            $"api/studies/studies/booked-slots?doctorId={doctorId}&from={QueryInstant(from)}&to={QueryInstant(to)}",
            cancellationToken) ?? [];

    public async Task<ApiOutcome> BookAsync(BookAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        using var response = await http.PostAsJsonAsync("api/studies/appointments", request, cancellationToken);
        return await ApiOutcome.FromAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<StudySummary>> ListStudiesAsync(CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<StudySummary>>("api/studies/studies", cancellationToken) ?? [];

    public async Task<StudyDetail?> GetStudyAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/studies/studies/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StudyDetail>(cancellationToken);
    }

    public async Task<ApiOutcome> ImportImagesAsync(
        Guid studyId,
        IReadOnlyList<UploadFile> files,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var content = new DeferredStreamContent(file);
            content.Headers.ContentType = new MediaTypeHeaderValue(DicomMediaType);
            form.Add(content, "files", file.FileName);
        }

        using var response = await http.PostAsync($"api/imaging/studies/{studyId}/images", form, cancellationToken);
        return await ApiOutcome.FromAsync(response, cancellationToken);
    }

    public async Task<IReadOnlyList<InstanceSummary>> ListInstancesAsync(Guid studyId, CancellationToken cancellationToken = default) =>
        await http.GetFromJsonAsync<IReadOnlyList<InstanceSummary>>($"api/imaging/studies/{studyId}/instances", cancellationToken) ?? [];

    public Task<ApiOutcome> OpenStudyAsync(Guid studyId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/studies/studies/{studyId}/open", cancellationToken);

    public Task<ApiOutcome> RenewLockAsync(Guid studyId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/studies/studies/{studyId}/lock/heartbeat", cancellationToken);

    public Task<ApiOutcome> ReleaseLockAsync(Guid studyId, CancellationToken cancellationToken = default) =>
        PostAsync($"api/studies/studies/{studyId}/lock/release", cancellationToken);

    public async Task<LockOwner?> GetLockOwnerAsync(Guid studyId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"api/studies/studies/{studyId}/lock", cancellationToken);
        response.EnsureSuccessStatusCode();

        return response.StatusCode == HttpStatusCode.NoContent
            ? null
            : await response.Content.ReadFromJsonAsync<LockOwner>(cancellationToken);
    }

    private async Task<ApiOutcome> PostAsync(string uri, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsync(uri, content: null, cancellationToken);
        return await ApiOutcome.FromAsync(response, cancellationToken);
    }

    private static string QueryInstant(DateTimeOffset instant) =>
        Uri.EscapeDataString(instant.ToString("O", CultureInfo.InvariantCulture));
}
