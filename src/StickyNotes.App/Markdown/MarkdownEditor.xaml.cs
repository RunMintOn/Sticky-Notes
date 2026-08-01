using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace StickyNotes.App.Markdown;

public partial class MarkdownEditor : UserControl
{
    private readonly MarkdownMarkerGenerator _markerGenerator;
    private readonly MarkdownColorizer _colorizer;
    private readonly MarkdownImageGenerator _imageGenerator;
    private readonly MarkdownListGenerator _listGenerator;
    private readonly MarkdownRuleGenerator _ruleGenerator;
    private readonly MarkdownCodeBlockLayer _codeBlockLayer;
    private MarkdownPresentation _presentation = new();
    private int _activeLine;
    private int _hoverLine;
    private bool _revealMarkersOnHover;
    private bool _overlayUpdatePending;
    private string _assetRoot = "";
    private string? _previewFullPath;
    private int _previewRequest;
    private CodeBlockAppearance _codeBlockAppearance = new(
        -4, -4, 3, 3, 5, 39, 24, 5, 7);

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
        _imageGenerator = new MarkdownImageGenerator(
            ImageOverlay,
            ShowImagePreview,
            delta => TextEditor.ScrollToVerticalOffset(Math.Max(
                0,
                TextEditor.VerticalOffset - Math.Sign(delta) *
                TextEditor.TextArea.TextView.DefaultLineHeight * 3)),
            ScheduleOverlays);
        _listGenerator = new MarkdownListGenerator(TextEditor.Document, IsLineRevealed);
        _ruleGenerator = new MarkdownRuleGenerator(TextEditor.Document, TextEditor.TextArea.TextView, IsLineRevealed);
        _codeBlockLayer = new MarkdownCodeBlockLayer(
            CodeBlockBackground,
            CodeBlockOverlay,
            (Style)FindResource("CodeCopyButton"));
        TextEditor.TextArea.TextView.ElementGenerators.Add(_markerGenerator);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_ruleGenerator);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_listGenerator);
        TextEditor.TextArea.TextView.ElementGenerators.Add(_imageGenerator);
        TextEditor.TextArea.TextView.BackgroundRenderers.Add(_imageGenerator);
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
        TextEditor.TextArea.TextView.VisualLinesChanged += (_, _) => ScheduleOverlays();
        TextEditor.TextArea.TextView.ScrollOffsetChanged += (_, _) => ScheduleOverlays();
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
        TextEditor.PreviewTextInput += TextEditor_PreviewTextInput;
    }

    public event EventHandler? TextChanged;
    public event EventHandler? PasteImageRequested;
    public event EventHandler? ImagePreviewSizeChanged;

    public string AssetRoot
    {
        set
        {
            _assetRoot = value;
            _imageGenerator.AssetRoot = value;
        }
    }

    public Size ImagePreviewSize
    {
        get => new(ImagePreviewCard.Width, ImagePreviewCard.Height);
        set
        {
            var workArea = SystemParameters.WorkArea;
            var maximumWidth = Math.Max(320, workArea.Width - 32);
            var maximumHeight = Math.Max(240, workArea.Height - 32);
            var minimumWidth = Math.Min(420, maximumWidth);
            var minimumHeight = Math.Min(320, maximumHeight);
            var width = double.IsFinite(value.Width) ? value.Width : 720;
            var height = double.IsFinite(value.Height) ? value.Height : 520;
            ImagePreviewCard.Width = Math.Clamp(width, minimumWidth, maximumWidth);
            ImagePreviewCard.Height = Math.Clamp(height, minimumHeight, maximumHeight);
        }
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

    public bool AutoContinueLists { get; set; } = true;

    public CodeBlockAppearance CodeBlockAppearance
    {
        get => _codeBlockAppearance;
        set
        {
            _codeBlockAppearance = value;
            _codeBlockLayer.Update(_presentation.CodeBlocks, value);
            TextEditor.TextArea.TextView.Redraw();
            ScheduleOverlays();
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

    public void ToggleBold() => ToggleCurrentLineOrSelection("**");
    public void ToggleItalic() => ToggleInline("*", MarkdownStyleKind.Italic);
    public void ToggleStrikethrough() => ToggleCurrentLineOrSelection("~~");
    public void ToggleHighlight() => ToggleCurrentLineOrSelection("==");
    public void ToggleInlineCode() => ToggleInline("`", MarkdownStyleKind.InlineCode);

    public void InsertMarkdownImage(string relativePath)
    {
        var document = TextEditor.Document;
        var insertionOffset = TextEditor.SelectionStart;
        var selectionEnd = insertionOffset + TextEditor.SelectionLength;
        var startLine = document.GetLineByOffset(insertionOffset);
        var endLine = document.GetLineByOffset(Math.Min(selectionEnd, document.TextLength));
        var textBefore = document.GetText(startLine.Offset, insertionOffset - startLine.Offset);
        var textAfter = document.GetText(selectionEnd, endLine.EndOffset - selectionEnd);
        var prefix = string.IsNullOrWhiteSpace(textBefore) ? "" : "\n";
        var suffix = string.IsNullOrWhiteSpace(textAfter) ? "" : "\n";
        var syntax = $"{prefix}![image]({relativePath}){suffix}";

        document.Replace(insertionOffset, TextEditor.SelectionLength, syntax);
        TextEditor.CaretOffset = insertionOffset + syntax.Length;
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

    private void ToggleCurrentLineOrSelection(string marker)
    {
        var result = MarkdownEditing.ToggleInline(
            TextEditor.Text,
            TextEditor.SelectionStart,
            TextEditor.SelectionLength,
            marker);
        if (result.Text == TextEditor.Text) return;
        TextEditor.Document.Replace(0, TextEditor.Document.TextLength, result.Text);
        TextEditor.Select(result.SelectionStart, result.SelectionLength);
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
        if (e.Key == Key.Escape && ImagePreviewPopup.IsOpen)
        {
            ImagePreviewPopup.IsOpen = false;
            e.Handled = true;
            return;
        }
        if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None && AutoContinueLists)
        {
            var result = MarkdownEditing.ContinueList(TextEditor.Text, TextEditor.CaretOffset);
            if (result is not null)
            {
                TextEditor.Document.Replace(0, TextEditor.Document.TextLength, result.Value.Text);
                TextEditor.CaretOffset = result.Value.SelectionStart;
                e.Handled = true;
                return;
            }
        }
        if (Keyboard.Modifiers != ModifierKeys.Control) return;
        if (e.Key == Key.V && ClipboardContainsImage())
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
        else if (e.Key == Key.H)
        {
            ToggleHighlight();
            e.Handled = true;
        }
        else if (e.Key == Key.D)
        {
            ToggleStrikethrough();
            e.Handled = true;
        }
    }

    private static bool ClipboardContainsImage()
    {
        try
        {
            return Clipboard.ContainsImage();
        }
        catch (ExternalException)
        {
            return false;
        }
    }

    private async void ShowImagePreview(MarkdownImageSpan image)
    {
        var request = ++_previewRequest;
        _previewFullPath = null;
        ImagePreviewName.Text = string.IsNullOrWhiteSpace(image.AltText) ? "Image" : image.AltText;
        ImagePreviewDetails.Text = "Loading…";
        ImagePreviewSource.Source = null;
        ImagePreviewUnavailable.Visibility = Visibility.Collapsed;
        OpenImageOriginalButton.IsEnabled = false;
        ImagePreviewScrim.Visibility = Visibility.Visible;
        ImagePreviewPopup.IsOpen = true;

        var preview = await Task.Run(() => MarkdownImagePreviewLoader.Load(_assetRoot, image));
        if (request != _previewRequest || !ImagePreviewPopup.IsOpen) return;

        ImagePreviewName.Text = preview.Name;
        ImagePreviewDetails.Text = preview.Details;
        ImagePreviewSource.Source = preview.Source;
        ImagePreviewUnavailable.Visibility = preview.Source is null
            ? Visibility.Visible
            : Visibility.Collapsed;
        _previewFullPath = preview.FullPath;
        OpenImageOriginalButton.IsEnabled = preview.FullPath is not null;
    }

    private void ImagePreviewResize_DragDelta(object sender, DragDeltaEventArgs e) =>
        ImagePreviewSize = new Size(
            ImagePreviewCard.Width + e.HorizontalChange,
            ImagePreviewCard.Height + e.VerticalChange);

    private void ImagePreviewResize_DragCompleted(object sender, DragCompletedEventArgs e) =>
        ImagePreviewSizeChanged?.Invoke(this, EventArgs.Empty);

    private void ImagePreviewScrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        ImagePreviewPopup.IsOpen = false;

    private void ImagePreviewPopup_Closed(object? sender, EventArgs e)
    {
        _previewRequest++;
        _previewFullPath = null;
        ImagePreviewScrim.Visibility = Visibility.Collapsed;
    }

    private void CloseImagePreview_Click(object sender, RoutedEventArgs e) =>
        ImagePreviewPopup.IsOpen = false;

    private void OpenImageOriginal_Click(object sender, RoutedEventArgs e)
    {
        if (_previewFullPath is null) return;
        try
        {
            Process.Start(new ProcessStartInfo(_previewFullPath) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // The system viewer is optional; a launch failure must not close the note.
        }
    }

    private void TextEditor_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (e.Text != "`" || TextEditor.SelectionLength == 0) return;
        var result = MarkdownEditing.TypeBacktick(
            TextEditor.Text,
            TextEditor.SelectionStart,
            TextEditor.SelectionLength);
        TextEditor.Document.Replace(0, TextEditor.Document.TextLength, result.Text);
        TextEditor.Select(result.SelectionStart, result.SelectionLength);
        e.Handled = true;
    }

    private bool IsLineRevealed(int line) => line == _activeLine || line == _hoverLine;

    private void RefreshPresentation()
    {
        _presentation = MarkdownPresentation.Parse(TextEditor.Text);
        _markerGenerator.Update(_presentation.Markers);
        _colorizer.Update(_presentation.Styles, _presentation.Markers);
        _imageGenerator.Update(_presentation.Images);
        _listGenerator.Update(_presentation.Lists);
        _ruleGenerator.Update(_presentation.Rules);
        _codeBlockLayer.Update(_presentation.CodeBlocks, CodeBlockAppearance);
        TextEditor.TextArea.TextView.Redraw();
        ScheduleOverlays();
    }

    private void ScheduleOverlays()
    {
        if (_overlayUpdatePending) return;
        _overlayUpdatePending = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _overlayUpdatePending = false;
            _imageGenerator.Refresh(TextEditor.TextArea.TextView);
            _codeBlockLayer.Refresh(TextEditor.TextArea.TextView, TextEditor.Document);
        });
    }
}
