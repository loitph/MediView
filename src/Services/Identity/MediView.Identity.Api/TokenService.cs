using MediView.Identity.Api.Entities;
using MediView.Shared;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MediView.Identity.Api;

public class TokenService(IConfiguration configuration)
{
    public string CreateToken(User user, Guid? doctorId)
    {
        var claims = new Dictionary<string, object>
        {
            [UserClaims.UserId] = user.Id.ToString(),
            [UserClaims.Name] = user.FullName,
            [UserClaims.Role] = user.Role.ToString(),
        };

        if (doctorId is not null)
        {
            claims[UserClaims.DoctorId] = doctorId.Value.ToString();
        }

        var token = new SecurityTokenDescriptor
        {
            Issuer = ServiceDefaults.JwtIssuer,
            Audience = ServiceDefaults.JwtAudience,
            Expires = DateTime.UtcNow.AddHours(8),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                ServiceDefaults.GetSigningKey(configuration),
                SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(token);
    }
}
