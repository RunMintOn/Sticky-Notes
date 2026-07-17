using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownMarkerGenerator : VisualLineElementGenerator
{
    private readonly TextDocument _document;
    private readonly Func<int, bool> _isLineRevealed;
    private IReadOnlyList<MarkdownMarkerSpan> _markers = [];

    internal MarkdownMarkerGenerator(TextDocument document, Func<int, bool> isLineRevealed)
    {
        _document = document;
        _isLineRevealed = isLineRevealed;
    }

    internal void Update(IReadOnlyList<MarkdownMarkerSpan> markers) => _markers = markers;

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var marker in _markers)
        {
            if (marker.Start < startOffset) continue;
            if (!_isLineRevealed(_document.GetLineByOffset(marker.Start).LineNumber)) return marker.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var marker = _markers.FirstOrDefault(candidate => candidate.Start == offset);
        if (marker.Length == 0 || _isLineRevealed(_document.GetLineByOffset(offset).LineNumber)) return null;
        return new InlineObjectElement(marker.Length, new Border
        {
            Width = 0,
            Height = 1,
            IsHitTestVisible = false,
            Visibility = Visibility.Hidden
        });
    }
}
