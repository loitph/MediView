using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MediView.Web.Auth;

public sealed class TokenStore(ProtectedSessionStorage storage)
{
    private const string StorageKey = "mediview.access-token";

    private string? token;
    private bool loaded;

    public async ValueTask<string?> GetAsync()
    {
        if (!loaded)
        {
            var stored = await storage.GetAsync<string>(StorageKey);
            token = stored.Success ? stored.Value : null;
            loaded = true;
        }

        return token;
    }

    public async ValueTask SetAsync(string accessToken)
    {
        await storage.SetAsync(StorageKey, accessToken);
        token = accessToken;
        loaded = true;
    }

    public async ValueTask ClearAsync()
    {
        await storage.DeleteAsync(StorageKey);
        token = null;
        loaded = true;
    }
}
