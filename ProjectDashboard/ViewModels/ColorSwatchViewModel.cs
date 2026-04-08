using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ProjectDashboard.ViewModels;

public partial class ColorSwatchViewModel : ObservableObject
{
    public Color Color { get; }
    public string HexValue { get; }

    [ObservableProperty]
    private bool isSelected;

    public IRelayCommand SelectCommand { get; }

    public ColorSwatchViewModel(string hex, Action<ColorSwatchViewModel> onSelect)
    {
        HexValue = hex;
        Color = Color.FromArgb(hex);
        SelectCommand = new RelayCommand(() => onSelect(this));
    }
}
