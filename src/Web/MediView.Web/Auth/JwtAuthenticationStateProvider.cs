using System.Security.Claims;
using MediView.Shared;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MediView.Web.Auth;

public class JwtAuthenticationStateProvider(TokenStore tokens) : AuthenticationStateProvider
{
    private static readonly AuthenticationState SignedOut = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await tokens.GetAsync();
        return token is null ? SignedOut : StateFromToken(token);
    }

    public async Task<ClaimsPrincipal> SignInAsync(string accessToken)
    {
        await tokens.SetAsync(accessToken);
        var state = StateFromToken(accessToken);
        NotifyAuthenticationStateChanged(Task.FromResult(state));
        return state.User;
    }

    public async Task SignOutAsync()
    {
        await tokens.ClearAsync();
        NotifyAuthenticationStateChanged(Task.FromResult(SignedOut));
    }

    private static AuthenticationState StateFromToken(string accessToken)
    {
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);
        if (jwt.ValidTo <= DateTime.UtcNow)
        {
            return SignedOut;
        }

        var identity = new ClaimsIdentity(jwt.Claims, "jwt", UserClaims.Name, UserClaims.Role);
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }
}
