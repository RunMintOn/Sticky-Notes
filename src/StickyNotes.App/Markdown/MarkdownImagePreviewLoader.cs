using System.IO;
using System.Windows.Media.Imaging;

namespace StickyNotes.App.Markdown;

internal readonly record struct MarkdownImagePreview(
    string Name,
    string Details,
    string? FullPath,
    BitmapSource? Source);

internal static class MarkdownImagePreviewLoader
{
    internal static MarkdownImagePreview Load(string assetRoot, MarkdownImageSpan image)
    {
        try
        {
            if (Uri.TryCreate(image.Url, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                return Unavailable(image, "Remote preview unavailable");
            }

            var normalized = Uri.UnescapeDataString(image.Url.Replace('/', Path.DirectorySeparatorChar));
            var fullPath = Path.IsPathRooted(normalized)
                ? Path.GetFullPath(normalized)
                : Path.GetFullPath(Path.Combine(assetRoot, normalized));
            if (!File.Exists(fullPath)) return Unavailable(image, "Image unavailable");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 840;
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            var info = new FileInfo(fullPath);
            return new MarkdownImagePreview(
                DisplayName(image, fullPath),
                $"{bitmap.PixelWidth} × {bitmap.PixelHeight} · {FormatBytes(info.Length)}",
                fullPath,
                bitmap);
        }
        catch (Exception)
        {
            // Image references and files are untrusted input. Preview failure must stay local.
            return Unavailable(image, "Image unavailable");
        }
    }

    private static MarkdownImagePreview Unavailable(MarkdownImageSpan image, string details) =>
        new(DisplayName(image, null), details, null, null);

    private static string DisplayName(MarkdownImageSpan image, string? fullPath)
    {
        try
        {
            var name = fullPath is null ? Path.GetFileName(image.Url) : Path.GetFileName(fullPath);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        catch (Exception)
        {
            // Fall back to alt text for malformed paths.
        }
        return string.IsNullOrWhiteSpace(image.AltText) ? "Image" : image.AltText;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1024 * 1024) return $"{bytes / 1024d / 1024d:0.#} MB";
        if (bytes >= 1024) return $"{bytes / 1024d:0.#} KB";
        return $"{bytes} B";
    }
}
