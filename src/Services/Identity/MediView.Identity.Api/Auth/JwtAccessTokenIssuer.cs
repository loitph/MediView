using MediView.BuildingBlocks.Api.Auth;
using MediView.Identity.Application.Auth;
using MediView.Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MediView.Identity.Api.Auth;

internal sealed class JwtAccessTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenIssuer
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    public AccessToken Issue(User user, Guid? doctorId)
    {
        var jwt = options.Value;
        var issuedAt = timeProvider.GetUtcNow();
        var expiresAt = issuedAt + jwt.Lifetime;

        var token = TokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            Claims = ClaimsFor(user, doctorId),
            SigningCredentials = new SigningCredentials(jwt.CreateSigningKey(), SecurityAlgorithms.HmacSha256),
        });

        return new AccessToken(token, expiresAt);
    }

    private static Dictionary<string, object> ClaimsFor(User user, Guid? doctorId)
    {
        var claims = new Dictionary<string, object>
        {
            [MediViewClaims.Subject] = user.Id.ToString(),
            [MediViewClaims.Email] = user.Email,
            [MediViewClaims.Name] = user.FullName,
            [MediViewClaims.Role] = user.Role.ToString(),
        };

        if (doctorId is { } id)
        {
            claims[MediViewClaims.DoctorId] = id.ToString();
        }

        return claims;
    }
}
