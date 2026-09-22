using System.Security.Claims;
using MediView.BuildingBlocks.Api.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MediView.Web.Auth;

public sealed class JwtAuthenticationStateProvider(TokenStore tokens, TimeProvider timeProvider)
    : AuthenticationStateProvider
{
    private const string AuthenticationType = "MediViewJwt";

    private static readonly JsonWebTokenHandler TokenHandler = new();
    private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAsync();
        return token is null ? Anonymous : StateFor(token);
    }

    public async Task SignInAsync(string accessToken)
    {
        await tokens.SetAsync(accessToken);
        NotifyAuthenticationStateChanged(Task.FromResult(StateFor(accessToken)));
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
    }

    private AuthenticationState StateFor(string accessToken)
    {
        var jwt = TokenHandler.ReadJsonWebToken(accessToken);
        if (jwt.ValidTo <= timeProvider.GetUtcNow().UtcDateTime)
        {
            return Anonymous;
        }

        var identity = new ClaimsIdentity(jwt.Claims, AuthenticationType, MediViewClaims.Name, MediViewClaims.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
