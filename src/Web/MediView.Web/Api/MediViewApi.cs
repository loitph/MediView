using System.Globalization;
using System.Net;

namespace MediView.Web.Api;

public sealed class MediViewApi(HttpClient http)
{
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

    private static string QueryInstant(DateTimeOffset instant) =>
        Uri.EscapeDataString(instant.ToString("O", CultureInfo.InvariantCulture));
}
