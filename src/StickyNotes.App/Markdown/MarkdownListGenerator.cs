using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownListGenerator : VisualLineElementGenerator
{
    private readonly TextDocument _document;
    private readonly Func<int, bool> _isLineRevealed;
    private IReadOnlyList<MarkdownListSpan> _lists = [];

    internal MarkdownListGenerator(TextDocument document, Func<int, bool> isLineRevealed)
    {
        _document = document;
        _isLineRevealed = isLineRevealed;
    }

    internal void Update(IReadOnlyList<MarkdownListSpan> lists) => _lists = lists;

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var list in _lists)
        {
            if (list.Start < startOffset) continue;
            if (!_isLineRevealed(_document.GetLineByOffset(list.Start).LineNumber)) return list.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var span = _lists.FirstOrDefault(list => list.Start == offset);
        if (span.Length == 0 || _isLineRevealed(_document.GetLineByOffset(offset).LineNumber)) return null;

        var label = new TextBlock
        {
            Text = span.DisplayText,
            Foreground = new SolidColorBrush(Color.FromRgb(238, 238, 238)),
            VerticalAlignment = VerticalAlignment.Center,
            IsHitTestVisible = false
        };
        label.SetResourceReference(TextBlock.FontSizeProperty, "EditorFontSize");
        return new InlineObjectElement(span.Length, label);
    }
}
