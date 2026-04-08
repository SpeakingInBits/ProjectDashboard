using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ProjectDashboard.Messages;
using ProjectDashboard.Services;

namespace ProjectDashboard.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string token = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordHidden))]
    private bool showToken;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isStatusVisible;

    public bool IsPasswordHidden => !ShowToken;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public async Task LoadAsync()
    {
        Token = await _settingsService.GetTokenAsync() ?? string.Empty;
    }

    [RelayCommand]
    private void ToggleShowToken() => ShowToken = !ShowToken;

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settingsService.SaveTokenAsync(Token);
        WeakReferenceMessenger.Default.Send(new TokenUpdatedMessage(HasToken: !string.IsNullOrWhiteSpace(Token)));
        await ShowStatusAsync("✓ Token saved");
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        await _settingsService.SaveTokenAsync(null);
        Token = string.Empty;
        WeakReferenceMessenger.Default.Send(new TokenUpdatedMessage(HasToken: false));
        await ShowStatusAsync("Token cleared");
    }

    private async Task ShowStatusAsync(string message)
    {
        StatusMessage = message;
        IsStatusVisible = true;
        await Task.Delay(2500);
        IsStatusVisible = false;
    }
}
