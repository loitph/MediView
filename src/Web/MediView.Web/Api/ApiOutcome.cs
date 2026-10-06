using System.Net;

namespace MediView.Web.Api;

public sealed record ApiOutcome(HttpStatusCode StatusCode, string? Problem)
{
    private const string ProblemMediaType = "application/problem+json";

    public bool Succeeded => Problem is null;

    internal static async Task<ApiOutcome> FromAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
        new(response.StatusCode, response.IsSuccessStatusCode ? null : await DescribeAsync(response, cancellationToken));

    private static async Task<string> DescribeAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentType?.MediaType != ProblemMediaType)
        {
            return $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
        }

        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(cancellationToken);
        var fieldErrors = problem?.Errors.SelectMany(field => field.Value).ToList() ?? [];

        return fieldErrors.Count > 0
            ? string.Join(" ", fieldErrors)
            : problem?.Detail ?? problem?.Title ?? "The request failed.";
    }
}
