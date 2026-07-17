using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace StickyNotes.App.Markdown;

internal sealed class MarkdownRuleGenerator : VisualLineElementGenerator
{
    private readonly TextDocument _document;
    private readonly TextView _textView;
    private readonly Func<int, bool> _isLineRevealed;
    private IReadOnlyList<MarkdownRuleSpan> _rules = [];

    internal MarkdownRuleGenerator(TextDocument document, TextView textView, Func<int, bool> isLineRevealed)
    {
        _document = document;
        _textView = textView;
        _isLineRevealed = isLineRevealed;
    }

    internal void Update(IReadOnlyList<MarkdownRuleSpan> rules) => _rules = rules;

    public override int GetFirstInterestedOffset(int startOffset)
    {
        foreach (var rule in _rules)
        {
            if (rule.Start < startOffset) continue;
            if (!_isLineRevealed(_document.GetLineByOffset(rule.Start).LineNumber)) return rule.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        var rule = _rules.FirstOrDefault(candidate => candidate.Start == offset);
        if (rule.Length == 0 || _isLineRevealed(_document.GetLineByOffset(offset).LineNumber)) return null;
        return new InlineObjectElement(rule.Length, new Border
        {
            Width = Math.Max(60, _textView.ActualWidth - 45),
            Height = 1,
            Margin = new Thickness(0, 9, 0, 7),
            Background = new SolidColorBrush(Color.FromRgb(91, 91, 91)),
            IsHitTestVisible = false
        });
    }
}
