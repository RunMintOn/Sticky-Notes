using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownImageGenerator : VisualLineElementGenerator, IBackgroundRenderer
{
    private const double MaximumImageHeight = 260;
    private const double LoadingHeight = 90;
    private const double UnavailableHeight = 58;
    private const double VerticalGap = 12;
    private readonly Canvas _overlay;
    private readonly Action<MarkdownImageSpan> _preview;
    private readonly Action<int> _scroll;
    private readonly Action _refreshRequested;
    private readonly Dictionary<string, Task<MarkdownImagePreview>> _loads =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<Task<MarkdownImagePreview>> _watchedLoads = [];
    private readonly Brush _syntaxBackground = FrozenBrush(Color.FromRgb(40, 40, 40));
    private readonly Brush _cardBackground = FrozenBrush(Color.FromRgb(37, 37, 37));
    private readonly Brush _cardBorder = FrozenBrush(Color.FromRgb(61, 61, 61));
    private readonly Brush _placeholderText = FrozenBrush(Color.FromRgb(170, 170, 170));
    private IReadOnlyList<MarkdownImageSpan> _images = [];
    private string _assetRoot = "";

    internal MarkdownImageGenerator(
        Canvas overlay,
        Action<MarkdownImageSpan> preview,
        Action<int> scroll,
        Action refreshRequested)
    {
        _overlay = overlay;
        _preview = preview;
        _scroll = scroll;
        _refreshRequested = refreshRequested;
    }

    internal string AssetRoot
    {
        set
        {
            if (string.Equals(_assetRoot, value, StringComparison.OrdinalIgnoreCase)) return;
            _assetRoot = value;
            _loads.Clear();
            lock (_watchedLoads) _watchedLoads.Clear();
        }
    }

    internal void Update(IReadOnlyList<MarkdownImageSpan> images) => _images = images;

    internal void Refresh(TextView textView)
    {
        _overlay.Children.Clear();
        if (textView.VisualLines.Count == 0) return;

        foreach (var span in _images.Where(image => image.IsStandalone))
        {
            var line = textView.VisualLines.FirstOrDefault(candidate =>
                candidate.FirstDocumentLine.Offset <= span.Start &&
                candidate.LastDocumentLine.EndOffset >= span.Start + span.Length);
            if (line is null) continue;

            var availableWidth = Math.Max(80, textView.ActualWidth - 4);
            var load = GetOrStartLoad(span);
            var card = load.IsCompletedSuccessfully
                ? CreateLoadedCard(span, load.Result, availableWidth)
                : CreatePlaceholderCard(
                    load.IsCompleted ? "Image unavailable" : "Loading image…",
                    availableWidth,
                    load.IsCompleted ? UnavailableHeight : LoadingHeight);
            if (!load.IsCompleted) WatchLoad(load, textView, span);

            var syntaxRows = Math.Max(1, line.TextLines.Count);
            var top = line.VisualTop - textView.VerticalOffset +
                      syntaxRows * textView.DefaultLineHeight + 5;
            var origin = textView.TranslatePoint(new Point(0, top), _overlay);
            Canvas.SetLeft(card, origin.X);
            Canvas.SetTop(card, origin.Y);
            _overlay.Children.Add(card);
        }
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        foreach (var image in _images)
        {
            var segment = new ImageSegment(image.Start, image.Length);
            foreach (var original in BackgroundGeometryBuilder.GetRectsForSegment(textView, segment))
            {
                var rectangle = original;
                rectangle.Inflate(3, 1);
                drawingContext.DrawRoundedRectangle(_syntaxBackground, null, rectangle, 4, 4);
            }
        }
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var image in _images)
        {
            if (!image.IsStandalone) continue;
            var imageEnd = image.Start + image.Length;
            if (imageEnd >= startOffset) return imageEnd;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var span = _images.FirstOrDefault(image =>
            image.IsStandalone && image.Start + image.Length == offset);
        if (span.Length == 0) return null;

        var textView = CurrentContext.TextView;
        var availableWidth = Math.Max(80, textView.ActualWidth - 4);
        var load = GetOrStartLoad(span);
        var cardHeight = load.IsCompletedSuccessfully
            ? GetCardSize(load.Result, availableWidth).Height
            : load.IsCompleted ? UnavailableHeight : LoadingHeight;
        if (!load.IsCompleted) WatchLoad(load, textView, span);

        // The zero-width object preserves every Markdown character. Its custom baseline keeps
        // the syntax at the top while its height reserves stable document space below it.
        var reservedHeight = textView.DefaultLineHeight + cardHeight + VerticalGap;
        return new ReservedSpaceElement(reservedHeight, textView.DefaultBaseline);
    }

    private Task<MarkdownImagePreview> GetOrStartLoad(MarkdownImageSpan span)
    {
        var key = _assetRoot + "\0" + span.Url;
        if (_loads.TryGetValue(key, out var existing)) return existing;
        var load = Task.Run(() => MarkdownImagePreviewLoader.Load(_assetRoot, span));
        _loads[key] = load;
        return load;
    }

    private async void WatchLoad(
        Task<MarkdownImagePreview> load,
        TextView textView,
        MarkdownImageSpan span)
    {
        lock (_watchedLoads)
        {
            if (!_watchedLoads.Add(load)) return;
        }
        try
        {
            await load.ConfigureAwait(false);
        }
        catch
        {
            // The loader is defensive, but an unexpected failure must remain local to the image.
        }
        finally
        {
            lock (_watchedLoads) _watchedLoads.Remove(load);
        }

        if (textView.Dispatcher.HasShutdownStarted) return;
        try
        {
            _ = textView.Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
            {
                textView.Redraw(span.Start, span.Length, DispatcherPriority.Render);
                _refreshRequested();
            });
        }
        catch (InvalidOperationException)
        {
            // The editor may be closing while a background image load completes.
        }
    }

    private FrameworkElement CreateLoadedCard(
        MarkdownImageSpan span,
        MarkdownImagePreview preview,
        double availableWidth)
    {
        if (preview.Source is null)
            return CreatePlaceholderCard("Image unavailable", availableWidth, UnavailableHeight);

        var size = GetCardSize(preview, availableWidth);
        var image = new Image
        {
            Source = preview.Source,
            Width = size.Width - 2,
            Height = size.Height - 2,
            Stretch = Stretch.Uniform,
            IsHitTestVisible = false
        };
        var imageFrame = new Border
        {
            Child = image,
            Width = size.Width,
            Height = size.Height,
            Background = _cardBackground,
            BorderBrush = _cardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Cursor = Cursors.Hand,
            Focusable = false,
            Tag = span,
            ToolTip = string.IsNullOrWhiteSpace(span.AltText)
                ? "Preview image"
                : $"Preview {span.AltText}"
        };
        AutomationProperties.SetName(imageFrame, imageFrame.ToolTip.ToString());
        imageFrame.MouseLeftButtonUp += Image_MouseLeftButtonUp;
        imageFrame.MouseWheel += Image_MouseWheel;
        return imageFrame;
    }

    private FrameworkElement CreatePlaceholderCard(string text, double width, double height)
    {
        var placeholder = new Border
        {
            Width = width,
            Height = height,
            Background = _cardBackground,
            BorderBrush = _cardBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = new TextBlock
            {
                Text = text,
                Foreground = _placeholderText,
                FontSize = 13,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        AutomationProperties.SetName(placeholder, text);
        placeholder.MouseWheel += Image_MouseWheel;
        return placeholder;
    }

    private static Size GetCardSize(MarkdownImagePreview preview, double availableWidth)
    {
        if (preview.Source is null) return new Size(availableWidth, UnavailableHeight);
        var contentWidth = Math.Max(1, availableWidth - 2);
        var sourceWidth = Math.Max(1, preview.Source.Width);
        var sourceHeight = Math.Max(1, preview.Source.Height);
        var scale = Math.Min(contentWidth / sourceWidth, MaximumImageHeight / sourceHeight);
        return new Size(
            Math.Max(3, sourceWidth * scale + 2),
            Math.Max(3, sourceHeight * scale + 2));
    }

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border { Tag: MarkdownImageSpan span } image)
            image.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => _preview(span));
        e.Handled = true;
    }

    private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _scroll(e.Delta);
        e.Handled = true;
    }

    private sealed class ReservedSpaceElement(double height, double baseline)
        : InlineObjectElement(0, new Border { Width = 0, Height = height, IsHitTestVisible = false })
    {
        public override TextRun CreateTextRun(int visualColumn, ITextRunConstructionContext context)
        {
            var run = (InlineObjectRun)base.CreateTextRun(visualColumn, context);
            return new ReservedSpaceRun(run.Length, run.Properties, Element, height, baseline);
        }
    }

    private sealed class ReservedSpaceRun(
        int length,
        TextRunProperties properties,
        UIElement element,
        double height,
        double baseline) : InlineObjectRun(length, properties, element)
    {
        public override TextEmbeddedObjectMetrics Format(double remainingParagraphWidth)
        {
            _ = base.Format(remainingParagraphWidth);
            return new TextEmbeddedObjectMetrics(0, height, baseline);
        }
    }

    private readonly record struct ImageSegment(int Offset, int Length) : ISegment
    {
        public int EndOffset => Offset + Length;
    }

    private static Brush FrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
