using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownImageGenerator : VisualLineElementGenerator
{
    private readonly Style _buttonStyle;
    private readonly Action<MarkdownImageSpan> _preview;
    private IReadOnlyList<MarkdownImageSpan> _images = [];

    internal MarkdownImageGenerator(Style buttonStyle, Action<MarkdownImageSpan> preview)
    {
        _buttonStyle = buttonStyle;
        _preview = preview;
    }

    internal void Update(IReadOnlyList<MarkdownImageSpan> images) => _images = images;

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var image in _images)
        {
            if (image.Start >= startOffset) return image.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var span = _images.FirstOrDefault(image => image.Start == offset);
        if (span.Length == 0) return null;

        var button = new Button
        {
            Content = "▧",
            Style = _buttonStyle,
            ToolTip = string.IsNullOrWhiteSpace(span.AltText)
                ? "Preview image"
                : $"Preview {span.AltText}",
            Focusable = false,
            IsTabStop = false,
            Tag = span
        };
        button.Click += Preview_Click;

        // The button visually replaces Markdown's leading '!'. The underlying document is unchanged.
        return new InlineObjectElement(1, button);
    }

    private void Preview_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: MarkdownImageSpan span } button)
        {
            // Opening the popup changes editor focus. Defer it until AvalonEdit finishes
            // dispatching the inline element click to avoid a re-entrant visual-line rebuild.
            button.Dispatcher.BeginInvoke(DispatcherPriority.Input, () => _preview(span));
        }
        e.Handled = true;
    }
}
