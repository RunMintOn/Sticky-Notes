using System.Windows;
using StickyNotes.App.Models;
using StickyNotes.App.Services;

namespace StickyNotes.App;

public partial class App : Application
{
    private readonly Dictionary<Guid, NoteWindow> _noteWindows = [];
    private NoteStore _store = null!;
    private UserSettings _settings = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settings = new UserSettings();
        _settings.ValuesChanged += Settings_ValuesChanged;
        ApplicationResourceUpdater.Apply(_settings);
        _store = new NoteStore();
        await _store.LoadAsync();

        var mainWindow = new MainWindow(_store, _settings, OpenNote, CloseNote, DeleteNote, CreateNote);
        MainWindow = mainWindow;
        mainWindow.Show();

        foreach (var note in _store.Notes.Where(note => note.IsOpen).ToArray())
            OpenNote(note);

        if (_store.Notes.Count == 0)
            CreateNote();

        if (e.Args.Contains("--settings", StringComparer.OrdinalIgnoreCase))
            mainWindow.ShowSettingsPage();
    }

    internal NoteItem CreateNote()
    {
        var note = _store.CreateNote();
        OpenNote(note);
        return note;
    }

    internal void BringOpenNotesToFront()
    {
        foreach (var window in _noteWindows.Values)
        {
            window.Show();
            NativeWindowStyle.BringToFront(window);
        }
    }

    internal void ShowHelpPage() => ((MainWindow)MainWindow).ShowHelpPage();

    private void OpenNote(NoteItem note)
    {
        if (_noteWindows.TryGetValue(note.Id, out var existing))
        {
            existing.Reveal();
            return;
        }

        note.IsOpen = true;
        var window = new NoteWindow(note, _store, _settings);
        _noteWindows.Add(note.Id, window);
        window.Closed += (_, _) =>
        {
            _noteWindows.Remove(note.Id);
            if (!window.WasDeleted)
            {
                note.IsOpen = false;
                _store.ScheduleSave();
            }
        };
        window.Show();
        window.Activate();
        _store.ScheduleSave();
    }

    private void CloseNote(NoteItem note)
    {
        if (_noteWindows.TryGetValue(note.Id, out var window))
            window.Close();
    }

    private void DeleteNote(NoteItem note)
    {
        if (_noteWindows.TryGetValue(note.Id, out var window))
            window.DeleteNote();
        else
            _store.Delete(note);
    }

    private void Settings_ValuesChanged(object? sender, EventArgs e) =>
        ApplicationResourceUpdater.Apply(_settings);

    protected override void OnExit(ExitEventArgs e)
    {
        if (_settings is not null)
        {
            _settings.ValuesChanged -= Settings_ValuesChanged;
            _settings.SaveNow();
        }
        _store?.SaveNow();
        base.OnExit(e);
    }
}
