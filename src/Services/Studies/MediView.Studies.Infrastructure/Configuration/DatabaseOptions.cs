using System.ComponentModel.DataAnnotations;
namespace MediView.Studies.Infrastructure.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required(
        AllowEmptyStrings = false,
        ErrorMessage = "ConnectionStrings:Postgres is missing. Did you run `dotnet user-secrets set`?"
    )]
    public string Postgres { get; init; } = string.Empty;
}