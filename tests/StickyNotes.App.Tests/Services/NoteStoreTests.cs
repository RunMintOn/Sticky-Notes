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
}
