using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StickyNotes.App.Models;

public sealed class NoteItem : INotifyPropertyChanged
{
    private string _plainText = "";
    private string _rtfBase64 = "";
    private string _markdown = "";
    private string _color = "Yellow";
    private DateTimeOffset _updatedAt = DateTimeOffset.Now;
    private bool _isOpen;
    private bool _isPinned;

    public Guid Id { get; init; } = Guid.NewGuid();

    public string PlainText
    {
        get => _plainText;
        set { if (Set(ref _plainText, value)) OnPropertyChanged(nameof(Preview)); }
    }

    public string Preview => string.IsNullOrWhiteSpace(PlainText) ? "New note" : PlainText.Trim();

    public string RtfBase64
    {
        get => _rtfBase64;
        set => Set(ref _rtfBase64, value);
    }

    public string Markdown
    {
        get => _markdown;
        set => Set(ref _markdown, value);
    }

    public string Color
    {
        get => _color;
        set => Set(ref _color, value);
    }

    public DateTimeOffset UpdatedAt
    {
        get => _updatedAt;
        set
        {
            if (Set(ref _updatedAt, value))
                OnPropertyChanged(nameof(UpdatedLabel));
        }
    }

    public string UpdatedLabel => UpdatedAt.Date == DateTimeOffset.Now.Date
        ? UpdatedAt.ToString("h:mm tt")
        : UpdatedAt.ToString("MMM d");

    public bool IsOpen
    {
        get => _isOpen;
        set => Set(ref _isOpen, value);
    }

    public bool IsPinned
    {
        get => _isPinned;
        set => Set(ref _isPinned, value);
    }

    public double Left { get; set; } = double.NaN;
    public double Top { get; set; } = double.NaN;
    public double Width { get; set; } = 570;
    public double Height { get; set; } = 424;

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
