using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;

namespace StickyNotes.App.Services;

public sealed class UserSettings : INotifyPropertyChanged
{
    private readonly string _filePath;
    private readonly DispatcherTimer _saveTimer;
    private double _overallScale = 1;
    private double _textScale = 1;
    private double _iconScale = 1;
    private string _renderingProfile = RenderingProfiles[0];
    private bool _revealMarkdownOnHover;

    public static IReadOnlyList<string> RenderingProfiles { get; } =
    [
        "Original-like ClearType",
        "Crisp aliased",
        "Balanced system text",
        "Smooth grayscale"
    ];

    public UserSettings()
    {
        var directory = AppDataDirectory.Resolve();
        _filePath = Path.Combine(directory, "settings.json");
        var legacyFilePath = Path.Combine(directory, "appearance.json");
        var loadPath = File.Exists(_filePath) ? _filePath : legacyFilePath;

        if (File.Exists(loadPath))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<SettingsValues>(File.ReadAllText(loadPath));
                if (saved is not null)
                {
                    _overallScale = Clamp(saved.OverallScale, 0.8, 1.2);
                    _textScale = Clamp(saved.TextScale, 0.75, 1.2);
                    _iconScale = Clamp(saved.IconScale, 0.75, 1.25);
                    if (saved.RenderingProfile is not null && RenderingProfiles.Contains(saved.RenderingProfile))
                        _renderingProfile = saved.RenderingProfile;
                    _revealMarkdownOnHover = saved.RevealMarkdownOnHover;
                }
            }
            catch (JsonException) { }
        }

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _saveTimer.Tick += (_, _) => Save();
        Apply();
    }

    public double OverallScale
    {
        get => _overallScale;
        set => Set(ref _overallScale, Clamp(value, 0.8, 1.2));
    }

    public double TextScale
    {
        get => _textScale;
        set => Set(ref _textScale, Clamp(value, 0.75, 1.2));
    }

    public double IconScale
    {
        get => _iconScale;
        set => Set(ref _iconScale, Clamp(value, 0.75, 1.25));
    }

    public IReadOnlyList<string> AvailableRenderingProfiles => RenderingProfiles;

    public string RenderingProfile
    {
        get => _renderingProfile;
        set
        {
            if (!RenderingProfiles.Contains(value) || _renderingProfile == value) return;
            _renderingProfile = value;
            Changed();
            OnPropertyChanged();
        }
    }

    public bool RevealMarkdownOnHover
    {
        get => _revealMarkdownOnHover;
        set
        {
            if (_revealMarkdownOnHover == value) return;
            _revealMarkdownOnHover = value;
            Changed();
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Reset()
    {
        _overallScale = _textScale = _iconScale = 1;
        _renderingProfile = RenderingProfiles[0];
        _revealMarkdownOnHover = false;
        Changed();
        OnPropertyChanged(nameof(OverallScale));
        OnPropertyChanged(nameof(TextScale));
        OnPropertyChanged(nameof(IconScale));
        OnPropertyChanged(nameof(RenderingProfile));
        OnPropertyChanged(nameof(RevealMarkdownOnHover));
    }

    private void Set(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (Math.Abs(field - value) < 0.001) return;
        field = value;
        Changed();
        OnPropertyChanged(propertyName);
    }

    private void Changed()
    {
        Apply();
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void Apply()
    {
        SetResource("TitleFontSize", 27 * TextScale);
        SetResource("ListFontSize", 18 * TextScale);
        SetResource("EditorFontSize", 18 * TextScale);
        SetResource("MenuFontSize", 17 * TextScale);
        SetResource("ChromeIconSize", 16 * IconScale);
        SetResource("ToolbarIconSize", 18 * IconScale);
        SetResource("HeaderHeight", 40 * OverallScale);
        SetResource("ToolbarHeight", 49 * OverallScale);
        SetResource("HeaderGridLength", new GridLength(40 * OverallScale));
        SetResource("ToolbarGridLength", new GridLength(49 * OverallScale));
        SetResource("ChromeButtonExtent", 45 * OverallScale);
        SetResource("FormatButtonWidth", 52 * OverallScale);
        SetResource("FormatButtonHeight", 48 * OverallScale);
        SetResource("NoteMenuWidth", 365 * OverallScale);
        SetResource("NoteMenuPaletteHeight", 54 * OverallScale);
        SetResource("NoteMenuRowHeight", 62 * OverallScale);
        SetResource("NoteCardHeight", 174 * OverallScale);
        SetResource("NoteCardMinHeight", 105 * OverallScale);

        var rendering = RenderingProfile switch
        {
            "Crisp aliased" => new TextRenderingValues(
                "Microsoft YaHei UI", TextFormattingMode.Display, TextRenderingMode.Aliased, TextHintingMode.Fixed),
            "Balanced system text" => new TextRenderingValues(
                "Segoe UI", TextFormattingMode.Display, TextRenderingMode.Auto, TextHintingMode.Fixed),
            "Smooth grayscale" => new TextRenderingValues(
                "Segoe UI", TextFormattingMode.Ideal, TextRenderingMode.Grayscale, TextHintingMode.Animated),
            _ => new TextRenderingValues(
                "Microsoft YaHei UI", TextFormattingMode.Display, TextRenderingMode.ClearType, TextHintingMode.Fixed)
        };
        SetResource("InterfaceFontFamily", new FontFamily(rendering.FontFamily));
        SetResource("EditorFontFamily", new FontFamily(rendering.FontFamily));
        SetResource("TextFormattingMode", rendering.FormattingMode);
        SetResource("TextRenderingMode", rendering.RenderingMode);
        SetResource("TextHintingMode", rendering.HintingMode);
    }

    private static void SetResource(string key, object value) =>
        Application.Current.Resources[key] = value;

    private void Save()
    {
        _saveTimer.Stop();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(
            new SettingsValues(OverallScale, TextScale, IconScale, RenderingProfile, RevealMarkdownOnHover),
            new JsonSerializerOptions { WriteIndented = true }));
    }

    private static double Clamp(double value, double min, double max) => Math.Max(min, Math.Min(max, value));
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record SettingsValues(
        double OverallScale,
        double TextScale,
        double IconScale,
        string? RenderingProfile = null,
        bool RevealMarkdownOnHover = false);

    private sealed record TextRenderingValues(
        string FontFamily,
        TextFormattingMode FormattingMode,
        TextRenderingMode RenderingMode,
        TextHintingMode HintingMode);
}
