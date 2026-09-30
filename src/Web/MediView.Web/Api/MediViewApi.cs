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
}
