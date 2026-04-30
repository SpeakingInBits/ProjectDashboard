using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ProjectDashboard.Messages;
using ProjectDashboard.Services;
using System.Collections.ObjectModel;

namespace ProjectDashboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    // ── Global / fallback token ──────────────────────────────────────────────

    [ObservableProperty]
    private string globalToken = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGlobalPasswordHidden))]
    private bool showGlobalToken;

    public bool IsGlobalPasswordHidden => !ShowGlobalToken;

    // ── Per-owner tokens ─────────────────────────────────────────────────────

    public ObservableCollection<OwnerTokenViewModel> OwnerTokens { get; } = [];

    // Add-new-token form fields
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOwnerTokenCommand))]
    private string newOwner = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddOwnerTokenCommand))]
    private string newToken = string.Empty;

    [ObservableProperty]
    private bool isAddFormVisible;

    // ── Status ───────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isStatusVisible;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task LoadAsync()
    {
        GlobalToken = await _settingsService.GetTokenAsync() ?? string.Empty;

        OwnerTokens.Clear();
        foreach (var owner in _settingsService.GetOwnerList())
        {
            var token = await _settingsService.GetOwnerTokenAsync(owner) ?? string.Empty;
            OwnerTokens.Add(CreateRow(owner, token));
        }
    }

    // ── Global token commands ────────────────────────────────────────────────

    [RelayCommand]
    private void ToggleShowGlobalToken() => ShowGlobalToken = !ShowGlobalToken;

    [RelayCommand]
    private async Task SaveGlobalAsync()
    {
        await _settingsService.SaveTokenAsync(GlobalToken);
        BroadcastTokenChange();
        await ShowStatusAsync("✓ Default token saved");
    }

    [RelayCommand]
    private async Task ClearGlobalAsync()
    {
        await _settingsService.SaveTokenAsync(null);
        GlobalToken = string.Empty;
        BroadcastTokenChange();
        await ShowStatusAsync("Default token cleared");
    }

    // ── Per-owner token commands ─────────────────────────────────────────────

    [RelayCommand]
    private void ShowAddForm()
    {
        NewOwner = string.Empty;
        NewToken = string.Empty;
        IsAddFormVisible = true;
    }

    [RelayCommand]
    private void HideAddForm() => IsAddFormVisible = false;

    [RelayCommand(CanExecute = nameof(CanAddOwnerToken))]
    private async Task AddOwnerTokenAsync()
    {
        var owner = NewOwner.Trim().TrimStart('@');
        if (OwnerTokens.Any(t => t.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase)))
        {
            await ShowStatusAsync("A token for that owner already exists — edit it in the list.");
            return;
        }

        await _settingsService.SaveOwnerTokenAsync(owner, NewToken.Trim());
        OwnerTokens.Add(CreateRow(owner, NewToken.Trim()));
        BroadcastTokenChange();
        IsAddFormVisible = false;
        await ShowStatusAsync($"✓ Token added for {owner}");
    }

    private bool CanAddOwnerToken() =>
        !string.IsNullOrWhiteSpace(NewOwner) && !string.IsNullOrWhiteSpace(NewToken);

    // ── Row callbacks ────────────────────────────────────────────────────────

    private async Task OnOwnerTokenSaved(OwnerTokenViewModel row)
    {
        await _settingsService.SaveOwnerTokenAsync(row.Owner, row.Token);
        BroadcastTokenChange();
        await ShowStatusAsync($"✓ Token updated for {row.Owner}");
    }

    private async Task OnOwnerTokenDeleted(OwnerTokenViewModel row)
    {
        await _settingsService.DeleteOwnerTokenAsync(row.Owner);
        OwnerTokens.Remove(row);
        BroadcastTokenChange();
        await ShowStatusAsync($"Token removed for {row.Owner}");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private OwnerTokenViewModel CreateRow(string owner, string token) =>
        new(owner, token, OnOwnerTokenDeleted, OnOwnerTokenSaved);

    private void BroadcastTokenChange()
    {
        var hasAny = !string.IsNullOrWhiteSpace(GlobalToken) || OwnerTokens.Count > 0;
        WeakReferenceMessenger.Default.Send(new TokenUpdatedMessage(HasToken: hasAny));
    }

    private async Task ShowStatusAsync(string message)
    {
        StatusMessage = message;
        IsStatusVisible = true;
        await Task.Delay(2500);
        IsStatusVisible = false;
    }
}
