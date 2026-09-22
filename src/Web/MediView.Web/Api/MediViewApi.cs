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
}
