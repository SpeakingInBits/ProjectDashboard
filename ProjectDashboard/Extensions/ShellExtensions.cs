using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace ProjectDashboard.Extensions;

public static class ShellExtensions
{
    // Provide DisplayAlertAsync extension to match caller expectation.
    public static Task DisplayAlertAsync(this Shell shell, string title, string message, string cancel)
        => shell.DisplayAlert(title, message, cancel);
}
