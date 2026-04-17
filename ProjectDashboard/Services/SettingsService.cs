namespace ProjectDashboard.Services;

public class SettingsService
{
    private const string TokenKey = "github_token";
    private const string BannerDismissedKey = "token_banner_dismissed";

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTokenAsync(string? token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
                SecureStorage.Default.Remove(TokenKey);
            else
                await SecureStorage.Default.SetAsync(TokenKey, token.Trim());
        }
        catch { }
    }

    public bool IsBannerDismissed() =>
        Preferences.Default.Get(BannerDismissedKey, false);

    public void DismissBanner() =>
        Preferences.Default.Set(BannerDismissedKey, true);
}
