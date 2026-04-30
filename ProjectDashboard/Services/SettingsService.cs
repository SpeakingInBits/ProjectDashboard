namespace ProjectDashboard.Services;

public class SettingsService
{
    private const string TokenKey = "github_token";
    private const string BannerDismissedKey = "token_banner_dismissed";
    private const string OwnerTokenPrefix = "github_token:";
    private const string OwnerListKey = "github_token_owners";

    // ── Global / fallback token (kept for backward compatibility) ────────────

    public async Task<string?> GetTokenAsync()
    {
        try { return await SecureStorage.Default.GetAsync(TokenKey); }
        catch { return null; }
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

    // ── Per-owner tokens ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the best token for the given owner: owner-specific first, then global fallback.
    /// </summary>
    public async Task<string?> GetTokenForOwnerAsync(string owner)
    {
        var specific = await GetOwnerTokenAsync(owner);
        if (!string.IsNullOrWhiteSpace(specific))
            return specific;
        return await GetTokenAsync();
    }

    public async Task<string?> GetOwnerTokenAsync(string owner)
    {
        try { return await SecureStorage.Default.GetAsync(OwnerTokenPrefix + owner.ToLowerInvariant()); }
        catch { return null; }
    }

    public async Task SaveOwnerTokenAsync(string owner, string token)
    {
        var key = OwnerTokenPrefix + owner.ToLowerInvariant();
        try { await SecureStorage.Default.SetAsync(key, token.Trim()); }
        catch { return; }

        var owners = GetOwnerList();
        if (!owners.Contains(owner, StringComparer.OrdinalIgnoreCase))
        {
            owners.Add(owner);
            SaveOwnerList(owners);
        }
    }

    public async Task DeleteOwnerTokenAsync(string owner)
    {
        try { SecureStorage.Default.Remove(OwnerTokenPrefix + owner.ToLowerInvariant()); }
        catch { }

        var owners = GetOwnerList();
        owners.RemoveAll(o => o.Equals(owner, StringComparison.OrdinalIgnoreCase));
        SaveOwnerList(owners);
        await Task.CompletedTask;
    }

    /// <summary>Returns all owner names that have a stored token.</summary>
    public List<string> GetOwnerList()
    {
        var raw = Preferences.Default.Get(OwnerListKey, string.Empty);
        if (string.IsNullOrWhiteSpace(raw)) return [];
        return [.. raw.Split(',', StringSplitOptions.RemoveEmptyEntries)];
    }

    private void SaveOwnerList(List<string> owners) =>
        Preferences.Default.Set(OwnerListKey, string.Join(',', owners));

    // ── Banner ───────────────────────────────────────────────────────────────

    public bool IsBannerDismissed() =>
        Preferences.Default.Get(BannerDismissedKey, false);

    public void DismissBanner() =>
        Preferences.Default.Set(BannerDismissedKey, true);
}
