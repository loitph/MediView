using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using MediView.Studies.Application.Booking;

namespace MediView.Studies.Infrastructure.Identity;

internal sealed class IdentityDirectoryClient(HttpClient http) : IIdentityDirectory
{
    private const string DateFormat = "yyyy-MM-dd";

    public Task<PatientSnapshot?> FindPatientAsync(Guid patientUserId, CancellationToken cancellationToken) =>
        GetOrNullAsync<PatientSnapshot>(new Uri($"internal/people/{patientUserId}", UriKind.Relative), cancellationToken);

    public Task<DoctorSnapshot?> FindDoctorAsync(Guid doctorId, CancellationToken cancellationToken) =>
        GetOrNullAsync<DoctorSnapshot>(new Uri($"internal/doctors/{doctorId}", UriKind.Relative), cancellationToken);

    public async Task<bool> OffersSlotAsync(Guid doctorId, DateTimeOffset start, CancellationToken cancellationToken)
    {
        var day = DateOnly.FromDateTime(start.UtcDateTime);
        var availability = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"doctors/{doctorId}/availability?from={day.AddDays(-1).ToString(DateFormat, CultureInfo.InvariantCulture)}&to={day.AddDays(1).ToString(DateFormat, CultureInfo.InvariantCulture)}"),
            UriKind.Relative);

        var slots = await GetOrNullAsync<List<OfferedSlot>>(availability, cancellationToken) ?? [];
        return slots.Exists(slot => slot.Start == start);
    }

    private async Task<T?> GetOrNullAsync<T>(Uri uri, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await http.GetAsync(uri, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private sealed record OfferedSlot(DateTimeOffset Start, DateTimeOffset End);
}
