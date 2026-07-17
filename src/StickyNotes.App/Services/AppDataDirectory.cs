using System.IO;

namespace StickyNotes.App.Services;

internal static class AppDataDirectory
{
    internal static string Resolve()
    {
        var overridePath = Environment.GetEnvironmentVariable("WIN_STICKY_NOTES_DATA_DIR");
        var directory = string.IsNullOrWhiteSpace(overridePath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinStickyNotes")
            : overridePath;
        Directory.CreateDirectory(directory);
        return directory;
    }
}
