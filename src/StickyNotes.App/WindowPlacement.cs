using System.Windows;

namespace StickyNotes.App;

public static class WindowPlacement
{
    private const double RequiredVisibleExtent = 48;

    public static Rect CurrentDesktop => new(
        SystemParameters.VirtualScreenLeft,
        SystemParameters.VirtualScreenTop,
        SystemParameters.VirtualScreenWidth,
        SystemParameters.VirtualScreenHeight);

    public static Rect EnsureAccessible(Rect requested, Rect desktop)
    {
        if (IsAccessible(requested, desktop)) return requested;

        var left = desktop.Left + Math.Max(0, (desktop.Width - requested.Width) / 2);
        var top = desktop.Top + Math.Max(0, (desktop.Height - requested.Height) / 2);
        return new Rect(left, top, requested.Width, requested.Height);
    }

    public static bool IsAccessible(Rect window, Rect desktop)
    {
        if (window.IsEmpty || desktop.IsEmpty ||
            !double.IsFinite(window.X) || !double.IsFinite(window.Y) ||
            !double.IsFinite(window.Width) || !double.IsFinite(window.Height))
            return false;

        var intersection = Rect.Intersect(window, desktop);
        return !intersection.IsEmpty &&
               intersection.Width >= Math.Min(RequiredVisibleExtent, window.Width) &&
               intersection.Height >= Math.Min(RequiredVisibleExtent, window.Height);
    }
}
