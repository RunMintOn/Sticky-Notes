using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace StickyNotes.App.Markdown;

public partial class MarkdownEditor : UserControl
{
    private readonly MarkdownMarkerGenerator _markerGenerator;
    private readonly MarkdownColorizer _colorizer;
    private readonly MarkdownImageGenerator _imageGenerator;
    private readonly MarkdownListGenerator _listGenerator;
    private MarkdownPresentation _presentation = new();
    private int _activeLine;
    private int _hoverLine;
    private bool _revealMarkersOnHover;

    public MarkdownEditor()
    {
        InitializeComponent();

        TextEditor.Options.EnableHyperlinks = false;
        TextEditor.Options.EnableEmailHyperlinks = false;
        TextEditor.Options.ConvertTabsToSpaces = true;
        TextEditor.Options.IndentationSize = 2;
        TextEditor.TextArea.TextView.Margin = new Thickness(15, 10, 8, 8);

        _markerGenerator = new MarkdownMarkerGenerator(TextEditor.Document, IsLineRevealed);
        _colorizer = new MarkdownColorizer();
        _imageGenerator = new MarkdownImageGenerator(TextEditor.Document, line => line == _activeLine);
        _listGenerator = new MarkdownListGenerator(TextEditor.Document, IsLineRevealed);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_markerGenerator);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_listGenerator);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_imageGenerator);
        TextEditor.TextArea.TextView.LineTransformers.Add(_colorizer);

        TextEditor.TextChanged += (_, _) =>
        {
            RefreshPresentation();
            TextChanged?.Invoke(this, EventArgs.Empty);
        };
        TextEditor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            var line = TextEditor.TextArea.Caret.Line;
            if (_activeLine == line) return;
            _activeLine = line;
            TextEditor.TextArea.TextView.Redraw();
        };
        TextEditor.TextArea.TextView.MouseMove += TextView_MouseMove;
        TextEditor.TextArea.TextView.MouseLeave += (_, _) =>
        {
            if (_hoverLine == 0) return;
            _hoverLine = 0;
            TextEditor.TextArea.TextView.Redraw();
        };
        TextEditor.GotKeyboardFocus += (_, _) =>
        {
            _activeLine = TextEditor.TextArea.Caret.Line;
            TextEditor.TextArea.TextView.Redraw();
        };
        TextEditor.LostKeyboardFocus += (_, _) =>
        {
            _activeLine = 0;
            TextEditor.TextArea.TextView.Redraw();
        };
        TextEditor.PreviewKeyDown += TextEditor_PreviewKeyDown;
    }

    public event EventHandler? TextChanged;
    public event EventHandler? PasteImageRequested;

    public string AssetRoot
    {
        set => _imageGenerator.AssetRoot = value;
    }

    public bool RevealMarkersOnHover
    {
        get => _revealMarkersOnHover;
        set
        {
            if (_revealMarkersOnHover == value) return;
            _revealMarkersOnHover = value;
            if (!value) _hoverLine = 0;
            TextEditor.TextArea.TextView.Redraw();
        }
    }

    public string Text
    {
        get => TextEditor.Text;
        set
        {
            if (TextEditor.Text == value) return;
            TextEditor.Text = value;
            RefreshPresentation();
        }
    }

    public new bool Focus() => TextEditor.Focus();

    public void ToggleBold() => ToggleInline("**", MarkdownStyleKind.Bold);
    public void ToggleItalic() => ToggleInline("*", MarkdownStyleKind.Italic);
    public void ToggleStrikethrough() => ToggleInline("~~", MarkdownStyleKind.Strikethrough);
    public void ToggleInlineCode() => ToggleInline("`", MarkdownStyleKind.InlineCode);

    public void InsertMarkdownImage(string relativePath)
    {
        var syntax = $"![image]({relativePath})";
        TextEditor.Document.Replace(TextEditor.SelectionStart, TextEditor.SelectionLength, syntax);
        TextEditor.CaretOffset = TextEditor.SelectionStart + syntax.Length;
    }

    public void ToggleBullets()
    {
        var document = TextEditor.Document;
        var startLine = document.GetLineByOffset(TextEditor.SelectionStart);
        var selectionEnd = Math.Max(TextEditor.SelectionStart, TextEditor.SelectionStart + TextEditor.SelectionLength - 1);
        var endLine = document.GetLineByOffset(selectionEnd);
        var lines = new List<DocumentLine>();
        for (var line = startLine; line is not null && line.LineNumber <= endLine.LineNumber; line = line.NextLine)
            lines.Add(line);

        var allBulleted = lines.All(line => document.GetText(line.Offset, Math.Min(2, line.Length)) == "- ");
        document.BeginUpdate();
        try
        {
            foreach (var line in lines.AsEnumerable().Reverse())
            {
                if (allBulleted) document.Remove(line.Offset, 2);
                else document.Insert(line.Offset, "- ");
            }
        }
        finally
        {
            document.EndUpdate();
        }
    }

    private void ToggleInline(string marker, MarkdownStyleKind kind)
    {
        var document = TextEditor.Document;
        var offset = TextEditor.SelectionStart;
        var length = TextEditor.SelectionLength;

        if (length > 0 && offset >= marker.Length && offset + length + marker.Length <= document.TextLength &&
            document.GetText(offset - marker.Length, marker.Length) == marker &&
            document.GetText(offset + length, marker.Length) == marker)
        {
            document.BeginUpdate();
            document.Remove(offset + length, marker.Length);
            document.Remove(offset - marker.Length, marker.Length);
            document.EndUpdate();
            TextEditor.Select(offset - marker.Length, length);
            return;
        }

        var containing = _presentation.Styles.FirstOrDefault(style =>
            style.Kind == kind && offset >= style.Start && offset <= style.End);
        if (length == 0 && containing.Length > 0 &&
            containing.Start >= marker.Length && containing.End + marker.Length <= document.TextLength)
        {
            document.BeginUpdate();
            document.Remove(containing.End, marker.Length);
            document.Remove(containing.Start - marker.Length, marker.Length);
            document.EndUpdate();
            TextEditor.CaretOffset = Math.Max(containing.Start - marker.Length, offset - marker.Length);
            return;
        }

        var selected = length == 0 ? "" : document.GetText(offset, length);
        document.Replace(offset, length, marker + selected + marker);
        if (length == 0) TextEditor.CaretOffset = offset + marker.Length;
        else TextEditor.Select(offset + marker.Length, length);
    }

    private void TextView_MouseMove(object sender, MouseEventArgs e)
    {
        if (!RevealMarkersOnHover) return;
        var textView = TextEditor.TextArea.TextView;
        var point = e.GetPosition(textView) + textView.ScrollOffset;
        var position = textView.GetPositionFloor(point);
        var line = position?.Line ?? 0;
        if (_hoverLine == line) return;
        _hoverLine = line;
        textView.Redraw();
    }

    private void TextEditor_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (e.Key == Key.V && Clipboard.ContainsImage())
        {
            PasteImageRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
            return;
        }
        if (e.Key == Key.B)
        {
            ToggleBold();
            e.Handled = true;
        }
        else if (e.Key == Key.I)
        {
            ToggleItalic();
            e.Handled = true;
        }
    }

    private bool IsLineRevealed(int line) => line == _activeLine || line == _hoverLine;

    private void RefreshPresentation()
    {
        _presentation = MarkdownPresentation.Parse(TextEditor.Text);
        _markerGenerator.Update(_presentation.Markers);
        _colorizer.Update(_presentation.Styles, _presentation.Markers);
        _imageGenerator.Update(_presentation.Images);
        _listGenerator.Update(_presentation.Lists);
        TextEditor.TextArea.TextView.Redraw();
    }
}
