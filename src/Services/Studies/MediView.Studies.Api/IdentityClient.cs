namespace MediView.Studies.Api;

public record PatientInfo(Guid UserId, string FullName, string Mrn);

public record DoctorInfo(Guid Id, string FullName);

public record SlotInfo(DateTimeOffset Start, DateTimeOffset End);

public class IdentityClient
{
    private readonly HttpClient http;

    public IdentityClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        this.http = http;
        var callerToken = httpContextAccessor.HttpContext!.Request.Headers.Authorization.ToString();
        http.DefaultRequestHeaders.Add("Authorization", callerToken);
    }

    public async Task<PatientInfo?> GetPatientAsync(Guid userId)
    {
        using var response = await http.GetAsync($"internal/people/{userId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<PatientInfo>();
    }

    public async Task<DoctorInfo?> GetDoctorAsync(Guid doctorId)
    {
        using var response = await http.GetAsync($"internal/doctors/{doctorId}");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DoctorInfo>();
    }

    public async Task<bool> DoctorOffersSlotAsync(Guid doctorId, DateTimeOffset start)
    {
        var day = DateOnly.FromDateTime(start.UtcDateTime);
        var url = $"doctors/{doctorId}/availability?from={day.AddDays(-1):yyyy-MM-dd}&to={day.AddDays(1):yyyy-MM-dd}";
        var slots = await http.GetFromJsonAsync<List<SlotInfo>>(url) ?? [];
        return slots.Any(slot => slot.Start == start);
    }
}
