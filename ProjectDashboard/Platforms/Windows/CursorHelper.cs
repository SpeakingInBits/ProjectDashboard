using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace ProjectDashboard;

/// <summary>
/// Sets UIElement.ProtectedCursor via reflection since it is a protected property.
/// </summary>
internal static class CursorHelper
{
    private static readonly System.Reflection.PropertyInfo? s_protectedCursorProp =
        typeof(UIElement).GetProperty(
            "ProtectedCursor",
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Public);

    public static void SetCursor(UIElement element, InputCursor cursor) =>
        s_protectedCursorProp?.SetValue(element, cursor);
}
