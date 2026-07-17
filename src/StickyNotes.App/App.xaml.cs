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
        _store = new NoteStore();
        await _store.LoadAsync();

        var mainWindow = new MainWindow(_store, _settings, OpenNote, CreateNote);
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

    private void OpenNote(NoteItem note)
    {
        if (_noteWindows.TryGetValue(note.Id, out var existing))
        {
            existing.Show();
            existing.Activate();
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

    protected override void OnExit(ExitEventArgs e)
    {
        _store?.SaveNow();
        base.OnExit(e);
    }
}
