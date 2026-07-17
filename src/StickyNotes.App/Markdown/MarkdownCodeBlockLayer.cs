using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

public readonly record struct CodeBlockAppearance(
    double LeftOffset,
    double RightOffset,
    double TopExtent,
    double BottomExtent,
    double CornerRadius,
    byte BackgroundShade,
    double CopyButtonSize,
    double CopyButtonTopOffset,
    double CopyButtonRightOffset);

internal sealed class MarkdownCodeBlockLayer
{
    private readonly Canvas _backgroundLayer;
    private readonly Canvas _controlLayer;
    private readonly Style _copyButtonStyle;
    private IReadOnlyList<MarkdownCodeBlockSpan> _blocks = [];
    private CodeBlockAppearance _appearance;

    internal MarkdownCodeBlockLayer(Canvas backgroundLayer, Canvas controlLayer, Style copyButtonStyle)
    {
        _backgroundLayer = backgroundLayer;
        _controlLayer = controlLayer;
        _copyButtonStyle = copyButtonStyle;
    }

    internal void Update(IReadOnlyList<MarkdownCodeBlockSpan> blocks, CodeBlockAppearance appearance)
    {
        _blocks = blocks;
        _appearance = appearance;
    }

    internal void Refresh(TextView textView, TextDocument document)
    {
        _backgroundLayer.Children.Clear();
        _controlLayer.Children.Clear();
        if (textView.VisualLines.Count == 0) return;

        foreach (var block in _blocks)
        {
            var rectangle = GetVisibleRectangle(textView, block, _appearance);
            if (rectangle is null) continue;
            var origin = textView.TranslatePoint(rectangle.Value.TopLeft, _backgroundLayer);

            var background = new Border
            {
                Width = rectangle.Value.Width,
                Height = rectangle.Value.Height,
                CornerRadius = new CornerRadius(_appearance.CornerRadius),
                Background = Shade(_appearance.BackgroundShade),
                IsHitTestVisible = false
            };
            Canvas.SetLeft(background, origin.X);
            Canvas.SetTop(background, origin.Y);
            _backgroundLayer.Children.Add(background);

            var controlOrigin = _backgroundLayer.TranslatePoint(origin, _controlLayer);
            var size = _appearance.CopyButtonSize;
            var copy = new Button
            {
                Content = "\uE8C8",
                Width = size,
                Height = size,
                Style = _copyButtonStyle,
                ToolTip = Application.Current.TryFindResource("CopyCodeText") ?? "Copy code",
                Tag = (document, block)
            };
            copy.Click += Copy_Click;
            Canvas.SetLeft(copy, controlOrigin.X + rectangle.Value.Width - size - _appearance.CopyButtonRightOffset);
            Canvas.SetTop(copy, controlOrigin.Y + _appearance.CopyButtonTopOffset);
            _controlLayer.Children.Add(copy);
        }
    }

    private static Rect? GetVisibleRectangle(
        TextView textView,
        MarkdownCodeBlockSpan block,
        CodeBlockAppearance appearance)
    {
        var blockEnd = block.BlockStart + block.BlockLength;
        var visibleLines = textView.VisualLines.Where(line =>
            line.LastDocumentLine.EndOffset >= block.BlockStart &&
            line.FirstDocumentLine.Offset < blockEnd).ToArray();
        if (visibleLines.Length == 0) return null;

        var first = visibleLines[0];
        var last = visibleLines[^1];
        var top = first.VisualTop - textView.VerticalOffset - appearance.TopExtent;
        var bottom = last.VisualTop + last.Height - textView.VerticalOffset + appearance.BottomExtent;
        var clippedTop = Math.Max(-appearance.TopExtent, top);
        var clippedBottom = Math.Min(textView.ActualHeight + appearance.BottomExtent, bottom);
        return new Rect(
            appearance.LeftOffset,
            clippedTop,
            Math.Max(0, textView.ActualWidth - appearance.LeftOffset - appearance.RightOffset),
            Math.Max(0, clippedBottom - clippedTop));
    }

    private static Brush Shade(byte shade)
    {
        var brush = new SolidColorBrush(Color.FromRgb(shade, shade, shade));
        brush.Freeze();
        return brush;
    }

    private static async void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ValueTuple<TextDocument, MarkdownCodeBlockSpan> value } button) return;
        try
        {
            Clipboard.SetText(value.Item1.GetText(value.Item2.Start, value.Item2.Length));
        }
        catch (ExternalException)
        {
            return;
        }
        button.Content = "\uE73E";
        await Task.Delay(900);
        if (button.IsLoaded) button.Content = "\uE8C8";
    }
}
