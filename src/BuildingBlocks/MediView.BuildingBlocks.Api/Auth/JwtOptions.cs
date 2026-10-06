using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace MediView.BuildingBlocks.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; init; } = "mediview-identity";
    public string Audience { get; init; } = "mediview";
    public string SigningKey { get; init; } = string.Empty;
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromHours(8);

    public bool HasStrongSigningKey => Encoding.UTF8.GetByteCount(SigningKey) >= MinimumSigningKeyBytes;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
