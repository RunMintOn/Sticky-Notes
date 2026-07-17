using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownImageGenerator : VisualLineElementGenerator
{
    private readonly TextDocument _document;
    private readonly Func<int, bool> _isLineActive;
    private readonly Dictionary<string, ImageSource?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<MarkdownImageSpan> _images = [];

    internal MarkdownImageGenerator(TextDocument document, Func<int, bool> isLineActive)
    {
        _document = document;
        _isLineActive = isLineActive;
    }

    internal string AssetRoot { get; set; } = "";
    internal void Update(IReadOnlyList<MarkdownImageSpan> images) => _images = images;

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var image in _images)
        {
            if (image.Start < startOffset) continue;
            if (!_isLineActive(_document.GetLineByOffset(image.Start).LineNumber)) return image.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var span = _images.FirstOrDefault(image => image.Start == offset);
        if (span.Length == 0 || _isLineActive(_document.GetLineByOffset(offset).LineNumber)) return null;

        var source = Load(span.Url);
        UIElement element = source is null
            ? new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
                Padding = new Thickness(10, 6, 10, 6),
                Child = new TextBlock
                {
                    Text = $"Image unavailable · {span.AltText}",
                    Foreground = new SolidColorBrush(Color.FromRgb(170, 170, 170)),
                    FontSize = 13
                }
            }
            : new Image
            {
                Source = source,
                MaxWidth = 440,
                MaxHeight = 260,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 5, 0, 5),
                IsHitTestVisible = false
            };
        return new InlineObjectElement(span.Length, element);
    }

    private ImageSource? Load(string url)
    {
        if (_cache.TryGetValue(url, out var cached)) return cached;
        ImageSource? source = null;
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) &&
                (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
            {
                // Remote loading is deliberately deferred; it must never block the editor thread.
                source = null;
            }
            else
            {
                var normalized = Uri.UnescapeDataString(url.Replace('/', Path.DirectorySeparatorChar));
                var path = Path.IsPathRooted(normalized) ? normalized : Path.Combine(AssetRoot, normalized);
                if (File.Exists(path))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.DecodePixelWidth = 880;
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    source = bitmap;
                }
            }
        }
        catch (Exception exception) when (exception is IOException or NotSupportedException or UriFormatException) { }

        _cache[url] = source;
        return source;
    }
}
