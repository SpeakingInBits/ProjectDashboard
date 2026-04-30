using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectDashboard.ViewModels;

/// <summary>Represents one owner → token row in the Settings token list.</summary>
public partial class OwnerTokenViewModel : ObservableObject
{
    private readonly Func<OwnerTokenViewModel, Task> _onDelete;
    private readonly Func<OwnerTokenViewModel, Task> _onSave;

    public string Owner { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordHidden))]
    private bool showToken;

    [ObservableProperty]
    private string token = string.Empty;

    [ObservableProperty]
    private bool isEditing;

    public bool IsPasswordHidden => !ShowToken;

    public string MaskedToken => token.Length > 8
        ? token[..4] + new string('•', token.Length - 8) + token[^4..]
        : new string('•', token.Length);

    public OwnerTokenViewModel(string owner, string token,
        Func<OwnerTokenViewModel, Task> onDelete,
        Func<OwnerTokenViewModel, Task> onSave)
    {
        Owner = owner;
        this.token = token;
        _onDelete = onDelete;
        _onSave = onSave;
    }

    [RelayCommand]
    private void ToggleShowToken() => ShowToken = !ShowToken;

    [RelayCommand]
    private void Edit() => IsEditing = true;

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsEditing = false;
        await _onSave(this);
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    [RelayCommand]
    private async Task DeleteAsync() => await _onDelete(this);
}
