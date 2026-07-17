using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace StickyNotes.App;

internal static class NativeWindowStyle
{
    private const int DwmWindowCornerPreference = 33;
    private const int Round = 2;

    internal static void EnableRoundedCorners(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)) return;
            var preference = Round;
            _ = DwmSetWindowAttribute(
                new WindowInteropHelper(window).Handle,
                DwmWindowCornerPreference,
                ref preference,
                sizeof(int));
        };
    }

    internal static void BringToFront(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
            _ = SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
