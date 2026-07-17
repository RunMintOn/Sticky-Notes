using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownColorizer : DocumentColorizingTransformer
{
    private static readonly Brush LinkBrush = new SolidColorBrush(Color.FromRgb(89, 192, 231));
    private static readonly Brush MarkerBrush = new SolidColorBrush(Color.FromRgb(145, 145, 145));
    private static readonly Brush CodeBackground = new SolidColorBrush(Color.FromRgb(68, 68, 68));
    private static readonly Brush QuoteBrush = new SolidColorBrush(Color.FromRgb(190, 190, 190));
    private static readonly Brush HighlightBrush = new SolidColorBrush(Color.FromRgb(117, 96, 18));
    private IReadOnlyList<MarkdownStyleSpan> _styles = [];
    private IReadOnlyList<MarkdownMarkerSpan> _markers = [];

    internal void Update(IReadOnlyList<MarkdownStyleSpan> styles, IReadOnlyList<MarkdownMarkerSpan> markers)
    {
        _styles = styles;
        _markers = markers;
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        foreach (var marker in _markers)
        {
            var start = Math.Max(marker.Start, line.Offset);
            var end = Math.Min(marker.End, line.EndOffset);
            if (start < end)
                ChangeLinePart(start, end, element => element.TextRunProperties.SetForegroundBrush(MarkerBrush));
        }

        foreach (var style in _styles)
        {
            var start = Math.Max(style.Start, line.Offset);
            var end = Math.Min(style.End, line.EndOffset);
            if (start >= end) continue;

            ChangeLinePart(start, end, element =>
            {
                switch (style.Kind)
                {
                    case MarkdownStyleKind.Bold:
                        element.TextRunProperties.SetTypeface(Typeface(element, FontWeights.Bold));
                        break;
                    case MarkdownStyleKind.Italic:
                        element.TextRunProperties.SetTypeface(new Typeface(
                            element.TextRunProperties.Typeface.FontFamily,
                            FontStyles.Italic,
                            element.TextRunProperties.Typeface.Weight,
                            element.TextRunProperties.Typeface.Stretch));
                        break;
                    case MarkdownStyleKind.Strikethrough:
                        element.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                        break;
                    case MarkdownStyleKind.Link:
                        element.TextRunProperties.SetForegroundBrush(LinkBrush);
                        element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                        break;
                    case MarkdownStyleKind.InlineCode:
                        element.TextRunProperties.SetTypeface(new Typeface(
                            new FontFamily("Cascadia Mono, Consolas"),
                            FontStyles.Normal,
                            FontWeights.Normal,
                            FontStretches.Normal));
                        element.TextRunProperties.SetBackgroundBrush(CodeBackground);
                        break;
                    case MarkdownStyleKind.Blockquote:
                        element.TextRunProperties.SetForegroundBrush(QuoteBrush);
                        element.TextRunProperties.SetTypeface(new Typeface(
                            element.TextRunProperties.Typeface.FontFamily,
                            FontStyles.Italic,
                            element.TextRunProperties.Typeface.Weight,
                            element.TextRunProperties.Typeface.Stretch));
                        break;
                    case MarkdownStyleKind.Highlight:
                        element.TextRunProperties.SetBackgroundBrush(HighlightBrush);
                        break;
                    case MarkdownStyleKind.Heading1:
                    case MarkdownStyleKind.Heading2:
                    case MarkdownStyleKind.Heading3:
                        var multiplier = style.Kind switch
                        {
                            MarkdownStyleKind.Heading1 => 1.45,
                            MarkdownStyleKind.Heading2 => 1.28,
                            _ => 1.14
                        };
                        element.TextRunProperties.SetTypeface(Typeface(element, FontWeights.SemiBold));
                        element.TextRunProperties.SetFontRenderingEmSize(
                            element.TextRunProperties.FontRenderingEmSize * multiplier);
                        break;
                }
            });
        }
    }

    private static Typeface Typeface(VisualLineElement element, FontWeight weight) => new(
        element.TextRunProperties.Typeface.FontFamily,
        element.TextRunProperties.Typeface.Style,
        weight,
        element.TextRunProperties.Typeface.Stretch);
}
