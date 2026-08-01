using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using StickyNotes.App.Models;

namespace StickyNotes.App.Services;

public enum NoteLoadStatus
{
    Loaded,
    NoData,
    RecoveredMissingPrimary,
    RecoveredInvalidPrimary,
    InvalidWithoutBackup
}

public sealed class NoteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;
    private CancellationTokenSource? _scheduledSave;

    public NoteStore(string? dataDirectory = null)
    {
        var directory = dataDirectory ?? AppDataDirectory.Resolve();
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "notes.json");
    }

    public ObservableCollection<NoteItem> Notes { get; } = [];

    public async Task<NoteLoadStatus> LoadAsync()
    {
        var backupPath = _filePath + ".backup";
        if (!File.Exists(_filePath))
        {
            if (!File.Exists(backupPath)) return NoteLoadStatus.NoData;
            await LoadFromAsync(backupPath);
            return NoteLoadStatus.RecoveredMissingPrimary;
        }

        try
        {
            await LoadFromAsync(_filePath);
            return NoteLoadStatus.Loaded;
        }
        catch (JsonException)
        {
            File.Copy(_filePath, _filePath + ".invalid", true);
            if (!File.Exists(backupPath)) return NoteLoadStatus.InvalidWithoutBackup;
            await LoadFromAsync(backupPath);
            return NoteLoadStatus.RecoveredInvalidPrimary;
        }
    }

    private async Task LoadFromAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var notes = await JsonSerializer.DeserializeAsync<List<NoteItem>>(stream, JsonOptions);
        if (notes is null) return;
        foreach (var note in notes)
        {
            note.Markdown = NormalizeLineEndings(note.Markdown);
            note.PlainText = NormalizeLineEndings(note.PlainText);
            Notes.Add(note);
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

    public bool UpdateContent(NoteItem note, string markdown, DateTimeOffset updatedAt)
    {
        var normalized = NormalizeLineEndings(markdown).TrimEnd('\n');
        var current = string.IsNullOrEmpty(note.Markdown) ? note.PlainText : note.Markdown;
        if (string.Equals(
                NormalizeLineEndings(current).TrimEnd('\n'),
                normalized,
                StringComparison.Ordinal))
            return false;

        note.Markdown = normalized;
        note.PlainText = Regex.Replace(
            normalized,
            @"!?\[([^\]]*)\]\([^)]*\)|[*_~`#>-]",
            "$1").Trim();
        note.RtfBase64 = "";
        note.UpdatedAt = updatedAt;
        var index = Notes.IndexOf(note);
        if (index > 0) Notes.Move(index, 0);
        ScheduleSave();
        return true;
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

    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    public void SaveNow()
    {
        _scheduledSave?.Cancel();
        var tempPath = _filePath + ".tmp";
        var backupPath = _filePath + ".backup";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonSerializer.Serialize(Notes.ToArray(), JsonOptions));
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, backupPath, ignoreMetadataErrors: true);
            else
                File.Move(tempPath, _filePath);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}
