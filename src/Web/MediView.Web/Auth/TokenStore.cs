using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MediView.Web.Auth;

public class TokenStore(ProtectedSessionStorage storage)
{
    private const string StorageKey = "mediview.access-token";

    private string? token;
    private bool loaded;

    public async Task<string?> GetAsync()
    {
        if (!loaded)
        {
            var stored = await storage.GetAsync<string>(StorageKey);
            token = stored.Success ? stored.Value : null;
            loaded = true;
        }

        return token;
    }

    public async Task SetAsync(string accessToken)
    {
        await storage.SetAsync(StorageKey, accessToken);
        token = accessToken;
        loaded = true;
    }

    public async Task ClearAsync()
    {
        await storage.DeleteAsync(StorageKey);
        token = null;
        loaded = true;
    }
}
