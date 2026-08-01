using System.IO;
using StickyNotes.App.Models;
using StickyNotes.App.Services;

namespace StickyNotes.App.Tests.Services;

public sealed class NoteStoreTests
{
    [Fact]
    public void ClosingAnUnchangedNoteDoesNotUpdateOrReorderIt()
    {
        var store = new NoteStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var newer = new NoteItem { Markdown = "newer", UpdatedAt = new DateTimeOffset(2026, 7, 17, 10, 0, 0, TimeSpan.Zero) };
        var note = new NoteItem { Markdown = "unchanged", UpdatedAt = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero) };
        store.Notes.Add(newer);
        store.Notes.Add(note);

        var changed = store.UpdateContent(note, "unchanged\r\n", new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

        Assert.False(changed);
        Assert.Equal(new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero), note.UpdatedAt);
        Assert.Equal(1, store.Notes.IndexOf(note));
        Assert.Equal("unchanged", note.Markdown);
    }

    [Fact]
    public void EditingNoteContentUpdatesAndMovesItToTheTop()
    {
        var store = new NoteStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var newer = new NoteItem { Markdown = "newer" };
        var note = new NoteItem { Markdown = "old", UpdatedAt = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero) };
        var editedAt = new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero);
        store.Notes.Add(newer);
        store.Notes.Add(note);

        var changed = store.UpdateContent(note, "**edited**", editedAt);

        Assert.True(changed);
        Assert.Equal("**edited**", note.Markdown);
        Assert.Equal("edited", note.PlainText);
        Assert.Equal(editedAt, note.UpdatedAt);
        Assert.Same(note, store.Notes[0]);
    }

    [Fact]
    public void EditingNormalizesAllLineEndingsToLf()
    {
        var store = new NoteStore(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
        var note = new NoteItem { Markdown = "old" };
        store.Notes.Add(note);

        store.UpdateContent(note, "first\r\nsecond\rthird\nfourth", DateTimeOffset.Now);

        Assert.Equal("first\nsecond\nthird\nfourth", note.Markdown);
        Assert.DoesNotContain('\r', note.Markdown);
    }

    [Fact]
    public async Task SavingKeepsThePreviousVersionAsBackup()
    {
        var directory = NewDataDirectory();
        try
        {
            var store = new NoteStore(directory);
            var note = new NoteItem { Markdown = "first", PlainText = "first", Left = 80, Top = 80 };
            store.Notes.Add(note);
            store.SaveNow();
            note.Markdown = note.PlainText = "second";
            store.SaveNow();

            File.Delete(Path.Combine(directory, "notes.json"));
            var restored = new NoteStore(directory);
            var status = await restored.LoadAsync();

            Assert.Equal(NoteLoadStatus.RecoveredMissingPrimary, status);
            Assert.Equal("first", Assert.Single(restored.Notes).Markdown);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidPrimaryFileIsPreservedAndRecoveredFromBackup()
    {
        var directory = NewDataDirectory();
        try
        {
            var store = new NoteStore(directory);
            var note = new NoteItem { Markdown = "recoverable", PlainText = "recoverable", Left = 80, Top = 80 };
            store.Notes.Add(note);
            store.SaveNow();
            note.Markdown = note.PlainText = "latest";
            store.SaveNow();
            var primaryPath = Path.Combine(directory, "notes.json");
            await File.WriteAllTextAsync(primaryPath, "not json");

            var restored = new NoteStore(directory);
            var status = await restored.LoadAsync();

            Assert.Equal(NoteLoadStatus.RecoveredInvalidPrimary, status);
            Assert.Equal("recoverable", Assert.Single(restored.Notes).Markdown);
            Assert.Equal("not json", await File.ReadAllTextAsync(primaryPath + ".invalid"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task InvalidPrimaryWithoutBackupIsReportedAndPreserved()
    {
        var directory = NewDataDirectory();
        try
        {
            var primaryPath = Path.Combine(directory, "notes.json");
            await File.WriteAllTextAsync(primaryPath, "not json");

            var restored = new NoteStore(directory);
            var status = await restored.LoadAsync();

            Assert.Equal(NoteLoadStatus.InvalidWithoutBackup, status);
            Assert.Empty(restored.Notes);
            Assert.Equal("not json", await File.ReadAllTextAsync(primaryPath + ".invalid"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string NewDataDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
