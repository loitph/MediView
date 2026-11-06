namespace MediView.Imaging.Api;

public class StudiesClient
{
    private readonly HttpClient http;

    public StudiesClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        this.http = http;
        var callerToken = httpContextAccessor.HttpContext!.Request.Headers.Authorization.ToString();
        http.DefaultRequestHeaders.Add("Authorization", callerToken);
    }

    public async Task<bool> StudyExistsAsync(Guid studyId)
    {
        using var response = await http.GetAsync($"studies/{studyId}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> MarkImagesImportedAsync(Guid studyId)
    {
        using var response = await http.PostAsync($"internal/studies/{studyId}/images-imported", null);
        return response.IsSuccessStatusCode;
    }
}
