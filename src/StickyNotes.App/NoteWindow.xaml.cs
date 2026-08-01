using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using StickyNotes.App.Models;
using StickyNotes.App.Services;

namespace StickyNotes.App;

public partial class NoteWindow : Window
{
    private readonly NoteItem _note;
    private readonly NoteStore _store;
    private readonly UserSettings _settings;
    private readonly AttachmentService _attachments = new();
    private readonly DispatcherTimer _saveTimer;
    private bool _isLoading = true;

    public NoteWindow(NoteItem note, NoteStore store, UserSettings settings)
    {
        _note = note;
        _store = store;
        _settings = settings;
        DataContext = note;
        InitializeComponent();
        NativeWindowStyle.EnableRoundedCorners(this);
        Editor.AssetRoot = _attachments.AssetRoot;
        Editor.RevealMarkersOnHover = settings.RevealMarkdownOnHover;
        Editor.AutoContinueLists = settings.AutoContinueLists;
        Editor.CodeBlockAppearance = settings.CodeBlockAppearance;
        Editor.ImagePreviewSize = new Size(settings.ImagePreviewWidth, settings.ImagePreviewHeight);
        Editor.ImagePreviewSizeChanged += Editor_ImagePreviewSizeChanged;
        Topmost = note.IsPinned;

        Width = Math.Max(note.Width, MinWidth);
        Height = Math.Max(note.Height, MinHeight);
        if (double.IsNaN(note.Left) || double.IsNaN(note.Top))
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        else
        {
            var placement = WindowPlacement.EnsureAccessible(
                new Rect(note.Left, note.Top, Width, Height),
                WindowPlacement.CurrentDesktop);
            Left = note.Left = placement.Left;
            Top = note.Top = placement.Top;
        }

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _saveTimer.Tick += (_, _) => SaveEditor();
        _settings.PropertyChanged += Settings_PropertyChanged;
        LoadEditor();
        _isLoading = false;
    }

    public bool WasDeleted { get; private set; }

    public void Reveal()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var placement = WindowPlacement.EnsureAccessible(
            new Rect(Left, Top, width, height),
            WindowPlacement.CurrentDesktop);
        Left = placement.Left;
        Top = placement.Top;
        Show();
        Activate();
    }

    private void LoadEditor()
    {
        Editor.Text = string.IsNullOrEmpty(_note.Markdown) ? _note.PlainText : _note.Markdown;
    }

    private void Editor_TextChanged(object sender, EventArgs e)
    {
        if (_isLoading) return;
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveEditor()
    {
        _saveTimer.Stop();
        _store.UpdateContent(_note, Editor.Text, DateTimeOffset.Now);
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        AnimateChrome(expanded: true);
        Editor.Focus();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (NoteMenuPopup.IsOpen) return;
        AnimateChrome(expanded: false);
    }

    private void WindowBounds_Changed(object sender, EventArgs e)
    {
        if (_isLoading || WindowState != WindowState.Normal) return;
        var bounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        if (!WindowPlacement.IsAccessible(bounds, WindowPlacement.CurrentDesktop)) return;
        _note.Left = Left;
        _note.Top = Top;
        _note.Width = ActualWidth;
        _note.Height = ActualHeight;
        _store.ScheduleSave();
    }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        UpdateMenuOffset();
        NoteMenuPopup.IsOpen = !NoteMenuPopup.IsOpen;
    }

    private void Pin_Click(object sender, RoutedEventArgs e)
    {
        _note.IsPinned = !_note.IsPinned;
        Topmost = _note.IsPinned;
        _store.ScheduleSave();
    }

    private void Color_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string color })
        {
            _note.Color = color;
            _store.ScheduleSave();
            NoteMenuPopup.IsOpen = false;
        }
    }

    private void NewNote_Click(object sender, RoutedEventArgs e) =>
        ((App)Application.Current).CreateNote();

    private void NotesList_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.MainWindow.Show();
        Application.Current.MainWindow.Activate();
    }

    private void Help_Click(object sender, RoutedEventArgs e) =>
        ((App)Application.Current).ShowHelpPage();

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.F1)
        {
            ((App)Application.Current).ShowHelpPage();
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.N &&
                 System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
        {
            ((App)Application.Current).CreateNote();
            e.Handled = true;
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
        => DeleteNote();

    public void DeleteNote()
    {
        WasDeleted = true;
        _store.Delete(_note);
        Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Strike_Click(object sender, RoutedEventArgs e)
    {
        Editor.ToggleStrikethrough();
        Editor.Focus();
    }

    private void Bold_Click(object sender, RoutedEventArgs e)
    {
        Editor.ToggleBold();
        Editor.Focus();
    }

    private void Italic_Click(object sender, RoutedEventArgs e)
    {
        Editor.ToggleItalic();
        Editor.Focus();
    }

    private void Bullets_Click(object sender, RoutedEventArgs e)
    {
        Editor.ToggleBullets();
        Editor.Focus();
    }

    private void InlineCode_Click(object sender, RoutedEventArgs e)
    {
        Editor.ToggleInlineCode();
        Editor.Focus();
    }
    private void Image_Click(object sender, RoutedEventArgs e)
    {
        InsertImage();
        Editor.Focus();
    }

    private void Editor_PasteImageRequested(object? sender, EventArgs e) => InsertImage();

    private void InsertImage()
    {
        var path = _attachments.ImportFromClipboardOrPicker(_note);
        if (path is not null) Editor.InsertMarkdownImage(path);
    }

    protected override void OnClosed(EventArgs e)
    {
        Editor.ImagePreviewSizeChanged -= Editor_ImagePreviewSizeChanged;
        _settings.PropertyChanged -= Settings_PropertyChanged;
        if (!_isLoading && !WasDeleted) SaveEditor();
        base.OnClosed(e);
    }

    private void Editor_ImagePreviewSizeChanged(object? sender, EventArgs e) =>
        _settings.SetImagePreviewSize(Editor.ImagePreviewSize.Width, Editor.ImagePreviewSize.Height);

    private void Settings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(UserSettings.ImagePreviewWidth) or nameof(UserSettings.ImagePreviewHeight))
            Editor.ImagePreviewSize = new Size(_settings.ImagePreviewWidth, _settings.ImagePreviewHeight);
        if (e.PropertyName == nameof(UserSettings.RevealMarkdownOnHover))
            Editor.RevealMarkersOnHover = _settings.RevealMarkdownOnHover;
        if (e.PropertyName == nameof(UserSettings.AutoContinueLists))
            Editor.AutoContinueLists = _settings.AutoContinueLists;
        if (e.PropertyName == nameof(UserSettings.CodeBlockAppearance))
            Editor.CodeBlockAppearance = _settings.CodeBlockAppearance;
        if (e.PropertyName == nameof(UserSettings.OverallScale))
        {
            if (IsActive)
            {
                SetChrome(expanded: true);
            }
            else
            {
                SetChrome(expanded: false);
            }
            if (NoteMenuPopup.IsOpen) UpdateMenuOffset();
        }
    }

    private void UpdateMenuOffset() =>
        NoteMenuPopup.HorizontalOffset = Resource("ChromeButtonExtent") * 2 - Resource("NoteMenuWidth");

    private void AnimateChrome(bool expanded)
    {
        var headerTarget = expanded ? Resource("HeaderHeight") : 8 * _settings.OverallScale;
        var toolbarTarget = expanded ? Resource("ToolbarHeight") : 0;
        var easing = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = TimeSpan.FromMilliseconds(150);

        Header.BeginAnimation(HeightProperty, new DoubleAnimation(Header.ActualHeight, headerTarget, duration) { EasingFunction = easing });
        Toolbar.BeginAnimation(HeightProperty, new DoubleAnimation(Toolbar.ActualHeight, toolbarTarget, duration) { EasingFunction = easing });
        Toolbar.BeginAnimation(OpacityProperty, new DoubleAnimation(Toolbar.Opacity, expanded ? 1 : 0, duration));
    }

    private void SetChrome(bool expanded)
    {
        Header.BeginAnimation(HeightProperty, null);
        Toolbar.BeginAnimation(HeightProperty, null);
        Toolbar.BeginAnimation(OpacityProperty, null);
        Header.Height = expanded ? Resource("HeaderHeight") : 8 * _settings.OverallScale;
        Toolbar.Height = expanded ? Resource("ToolbarHeight") : 0;
        Toolbar.Opacity = expanded ? 1 : 0;
    }

    private static double Resource(string key) => (double)Application.Current.Resources[key];
}
