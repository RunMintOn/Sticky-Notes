using System.Globalization;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using StickyNotes.App.Models;
using StickyNotes.App.Services;

namespace StickyNotes.App;

public partial class MainWindow : Window
{
    private readonly NoteStore _store;
    private readonly Action<NoteItem> _openNote;
    private readonly Func<NoteItem> _createNote;

    public MainWindow(
        NoteStore store,
        UserSettings settings,
        Action<NoteItem> openNote,
        Func<NoteItem> createNote)
    {
        _store = store;
        _openNote = openNote;
        _createNote = createNote;
        Settings = settings;
        NotesView = CollectionViewSource.GetDefaultView(store.Notes);
        DataContext = this;
        InitializeComponent();
        NativeWindowStyle.EnableRoundedCorners(this);
    }

    public ICollectionView NotesView { get; }
    public UserSettings Settings { get; }

    private void AddNote_Click(object sender, RoutedEventArgs e) => _createNote();

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Settings_Click(object sender, RoutedEventArgs e) => ShowSettingsPage();

    internal void ShowSettingsPage()
    {
        NotesPage.Visibility = Visibility.Collapsed;
        SettingsPage.Visibility = Visibility.Visible;
        AddNoteButton.Visibility = Visibility.Collapsed;
        SettingsButton.Visibility = Visibility.Collapsed;
    }

    private void SettingsBack_Click(object sender, RoutedEventArgs e)
    {
        SettingsPage.Visibility = Visibility.Collapsed;
        NotesPage.Visibility = Visibility.Visible;
        AddNoteButton.Visibility = Visibility.Visible;
        SettingsButton.Visibility = Visibility.Visible;
    }

    private void SettingsReset_Click(object sender, RoutedEventArgs e) => Settings.Reset();

    private void NotesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (NotesList.SelectedItem is NoteItem note) _openNote(note);
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var query = SearchBox.Text.Trim();
        NotesView.Filter = item => item is NoteItem note &&
            (query.Length == 0 || note.PlainText.Contains(query, StringComparison.CurrentCultureIgnoreCase));
    }
}

public sealed class NoteColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (value as string) switch
        {
            "Green" => new SolidColorBrush(Color.FromRgb(101, 186, 90)),
            "Pink" => new SolidColorBrush(Color.FromRgb(234, 134, 194)),
            "Purple" => new SolidColorBrush(Color.FromRgb(176, 127, 224)),
            "Blue" => new SolidColorBrush(Color.FromRgb(89, 192, 231)),
            "Gray" => new SolidColorBrush(Color.FromRgb(152, 152, 152)),
            "Charcoal" => new SolidColorBrush(Color.FromRgb(72, 72, 72)),
            _ => new SolidColorBrush(Color.FromRgb(230, 185, 4))
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
