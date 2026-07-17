using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using StickyNotes.App.Models;

namespace StickyNotes.App.Services;

public sealed class NoteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;
    private CancellationTokenSource? _scheduledSave;

    public NoteStore()
    {
        var directory = AppDataDirectory.Resolve();
        _filePath = Path.Combine(directory, "notes.json");
    }

    public ObservableCollection<NoteItem> Notes { get; } = [];

    public async Task LoadAsync()
    {
        if (!File.Exists(_filePath)) return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var notes = await JsonSerializer.DeserializeAsync<List<NoteItem>>(stream, JsonOptions);
            if (notes is null) return;
            foreach (var note in notes) Notes.Add(note);
        }
        catch (JsonException)
        {
            File.Copy(_filePath, _filePath + ".invalid", true);
        }
    }

    public NoteItem CreateNote()
    {
        var offset = Notes.Count % 8 * 24;
        var note = new NoteItem { Left = 80 + offset, Top = 80 + offset, IsOpen = true };
        Notes.Insert(0, note);
        ScheduleSave();
        return note;
    }

    public void Delete(NoteItem note)
    {
        Notes.Remove(note);
        ScheduleSave();
    }

    public void ScheduleSave()
    {
        _scheduledSave?.Cancel();
        _scheduledSave = new CancellationTokenSource();
        _ = SaveAfterDelayAsync(_scheduledSave.Token);
    }

    private async Task SaveAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(400, cancellationToken);
            SaveNow();
        }
        catch (OperationCanceledException) { }
    }

    public void SaveNow()
    {
        _scheduledSave?.Cancel();
        var tempPath = _filePath + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(Notes.ToArray(), JsonOptions));
        File.Move(tempPath, _filePath, true);
    }
}
