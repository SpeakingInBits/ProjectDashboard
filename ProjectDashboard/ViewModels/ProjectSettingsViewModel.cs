using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectDashboard.Services;
using System.Collections.ObjectModel;

namespace ProjectDashboard.ViewModels;

public partial class ProjectSettingsViewModel : ObservableObject
{
    private static readonly string[] ColorPalette =
    [
        "#4A90D9", // Blue
        "#1565C0", // Dark Blue
        "#9B59B6", // Purple
        "#6A1B9A", // Deep Purple
        "#27AE60", // Green
        "#1B5E20", // Dark Green
        "#E67E22", // Orange
        "#E65100", // Deep Orange
        "#E91E63", // Pink
        "#880E4F", // Dark Pink
        "#00BCD4", // Cyan
        "#006064", // Dark Cyan
        "#FF5722", // Red Orange
        "#B71C1C", // Dark Red
        "#009688", // Teal
        "#004D40", // Dark Teal
        "#795548", // Brown
        "#3E2723", // Dark Brown
        "#607D8B", // Blue Grey
        "#263238", // Dark Blue Grey
        "#F9A825", // Amber
        "#FF6F00", // Dark Amber
        "#8BC34A", // Light Green
        "#33691E", // Olive Green
    ];

    private readonly DatabaseService _databaseService;
    private ProjectCardViewModel? _card;

    [ObservableProperty]
    private Color selectedColor = Colors.Transparent;

    [ObservableProperty]
    private string projectDisplayName = string.Empty;

    public ObservableCollection<ColorSwatchViewModel> ColorSwatches { get; } = [];

    public ProjectSettingsViewModel(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public void Initialize(ProjectCardViewModel card)
    {
        _card = card;
        ProjectDisplayName = card.DisplayName;
        SelectedColor = card.CardAccentColor;

        ColorSwatches.Clear();
        foreach (var hex in ColorPalette)
            ColorSwatches.Add(new ColorSwatchViewModel(hex, OnSwatchSelected));

        var current = card.Project.CardColor.ToUpperInvariant();
        foreach (var swatch in ColorSwatches)
            swatch.IsSelected = swatch.HexValue.Equals(current, StringComparison.OrdinalIgnoreCase);
    }

    private void OnSwatchSelected(ColorSwatchViewModel selected)
    {
        foreach (var swatch in ColorSwatches)
            swatch.IsSelected = swatch == selected;

        SelectedColor = selected.Color;
    }

    [RelayCommand]
    private void Randomize()
    {
        var unselected = ColorSwatches.Where(s => !s.IsSelected).ToList();
        if (unselected.Count == 0) return;
        var pick = unselected[Random.Shared.Next(unselected.Count)];
        OnSwatchSelected(pick);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_card is null) return;

        var selected = ColorSwatches.FirstOrDefault(s => s.IsSelected);
        if (selected is not null)
        {
            _card.UpdateColor(selected.Color, selected.HexValue);
            await _databaseService.SaveProjectAsync(_card.Project);
        }

        await Shell.Current.Navigation.PopModalAsync();
    }

    [RelayCommand]
    private static async Task CloseAsync() =>
        await Shell.Current.Navigation.PopModalAsync();
}
