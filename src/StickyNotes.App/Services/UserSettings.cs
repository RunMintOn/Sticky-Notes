using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Threading;
using StickyNotes.App.Markdown;

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
    private bool _autoContinueLists = true;
    private string _language = "English";
    private double _codeBlockLeftOffset = -4;
    private double _codeBlockRightOffset = -4;
    private double _codeBlockTopExtent = 3;
    private double _codeBlockBottomExtent = 3;
    private double _codeBlockCornerRadius = 5;
    private double _codeBlockBackgroundShade = 39;
    private double _codeBlockCopyButtonSize = 24;
    private double _codeBlockCopyButtonTopOffset = 5;
    private double _codeBlockCopyButtonRightOffset = 7;
    private double _imagePreviewWidth = 720;
    private double _imagePreviewHeight = 520;

    public static IReadOnlyList<string> RenderingProfiles { get; } =
    [
        "Original-like ClearType",
        "Crisp aliased",
        "Balanced system text",
        "Smooth grayscale"
    ];
    public static IReadOnlyList<string> Languages { get; } = ["English", "中文"];

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
                    _autoContinueLists = saved.AutoContinueLists;
                    if (saved.Language is not null && Languages.Contains(saved.Language))
                        _language = saved.Language;
                    _codeBlockLeftOffset = Clamp(saved.CodeBlockLeftOffset, -16, 24);
                    _codeBlockRightOffset = Clamp(saved.CodeBlockRightOffset, -16, 24);
                    _codeBlockTopExtent = Clamp(saved.CodeBlockTopExtent, 0, 20);
                    _codeBlockBottomExtent = Clamp(saved.CodeBlockBottomExtent, 0, 20);
                    _codeBlockCornerRadius = Clamp(saved.CodeBlockCornerRadius, 0, 16);
                    _codeBlockBackgroundShade = Clamp(saved.CodeBlockBackgroundShade, 20, 60);
                    _codeBlockCopyButtonSize = Clamp(saved.CodeBlockCopyButtonSize, 18, 36);
                    _codeBlockCopyButtonTopOffset = Clamp(saved.CodeBlockCopyButtonTopOffset, 0, 20);
                    _codeBlockCopyButtonRightOffset = Clamp(saved.CodeBlockCopyButtonRightOffset, 0, 24);
                    _imagePreviewWidth = Clamp(saved.ImagePreviewWidth ?? 720, 420, 3840);
                    _imagePreviewHeight = Clamp(saved.ImagePreviewHeight ?? 520, 320, 2160);
                }
            }
            catch (JsonException) { }
        }

        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _saveTimer.Tick += (_, _) => Save();
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

    public bool AutoContinueLists
    {
        get => _autoContinueLists;
        set
        {
            if (_autoContinueLists == value) return;
            _autoContinueLists = value;
            Changed();
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> AvailableLanguages => Languages;

    public string Language
    {
        get => _language;
        set
        {
            if (!Languages.Contains(value) || _language == value) return;
            _language = value;
            Changed();
            OnPropertyChanged();
        }
    }

    public double CodeBlockLeftOffset { get => _codeBlockLeftOffset; set => SetCodeBlock(ref _codeBlockLeftOffset, Clamp(value, -16, 24)); }
    public double CodeBlockRightOffset { get => _codeBlockRightOffset; set => SetCodeBlock(ref _codeBlockRightOffset, Clamp(value, -16, 24)); }
    public double CodeBlockTopExtent { get => _codeBlockTopExtent; set => SetCodeBlock(ref _codeBlockTopExtent, Clamp(value, 0, 20)); }
    public double CodeBlockBottomExtent { get => _codeBlockBottomExtent; set => SetCodeBlock(ref _codeBlockBottomExtent, Clamp(value, 0, 20)); }
    public double CodeBlockCornerRadius { get => _codeBlockCornerRadius; set => SetCodeBlock(ref _codeBlockCornerRadius, Clamp(value, 0, 16)); }
    public double CodeBlockBackgroundShade { get => _codeBlockBackgroundShade; set => SetCodeBlock(ref _codeBlockBackgroundShade, Clamp(value, 20, 60)); }
    public double CodeBlockCopyButtonSize { get => _codeBlockCopyButtonSize; set => SetCodeBlock(ref _codeBlockCopyButtonSize, Clamp(value, 18, 36)); }
    public double CodeBlockCopyButtonTopOffset { get => _codeBlockCopyButtonTopOffset; set => SetCodeBlock(ref _codeBlockCopyButtonTopOffset, Clamp(value, 0, 20)); }
    public double CodeBlockCopyButtonRightOffset { get => _codeBlockCopyButtonRightOffset; set => SetCodeBlock(ref _codeBlockCopyButtonRightOffset, Clamp(value, 0, 24)); }

    public double ImagePreviewWidth => _imagePreviewWidth;
    public double ImagePreviewHeight => _imagePreviewHeight;

    public void SetImagePreviewSize(double width, double height)
    {
        width = Clamp(width, 420, 3840);
        height = Clamp(height, 320, 2160);
        if (Math.Abs(_imagePreviewWidth - width) < 0.5 &&
            Math.Abs(_imagePreviewHeight - height) < 0.5) return;
        _imagePreviewWidth = width;
        _imagePreviewHeight = height;
        Changed();
        OnPropertyChanged(nameof(ImagePreviewWidth));
        OnPropertyChanged(nameof(ImagePreviewHeight));
    }

    public CodeBlockAppearance CodeBlockAppearance => new(
        CodeBlockLeftOffset,
        CodeBlockRightOffset,
        CodeBlockTopExtent,
        CodeBlockBottomExtent,
        CodeBlockCornerRadius,
        (byte)Math.Round(CodeBlockBackgroundShade),
        CodeBlockCopyButtonSize,
        CodeBlockCopyButtonTopOffset,
        CodeBlockCopyButtonRightOffset);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? ValuesChanged;

    public void Reset()
    {
        _overallScale = _textScale = _iconScale = 1;
        _renderingProfile = RenderingProfiles[0];
        _revealMarkdownOnHover = false;
        _autoContinueLists = true;
        _codeBlockLeftOffset = _codeBlockRightOffset = -4;
        _codeBlockTopExtent = _codeBlockBottomExtent = 3;
        _codeBlockCornerRadius = 5;
        _codeBlockBackgroundShade = 39;
        _codeBlockCopyButtonSize = 24;
        _codeBlockCopyButtonTopOffset = 5;
        _codeBlockCopyButtonRightOffset = 7;
        _imagePreviewWidth = 720;
        _imagePreviewHeight = 520;
        Changed();
        OnPropertyChanged(nameof(OverallScale));
        OnPropertyChanged(nameof(TextScale));
        OnPropertyChanged(nameof(IconScale));
        OnPropertyChanged(nameof(RenderingProfile));
        OnPropertyChanged(nameof(RevealMarkdownOnHover));
        OnPropertyChanged(nameof(AutoContinueLists));
        OnPropertyChanged(nameof(CodeBlockLeftOffset));
        OnPropertyChanged(nameof(CodeBlockRightOffset));
        OnPropertyChanged(nameof(CodeBlockTopExtent));
        OnPropertyChanged(nameof(CodeBlockBottomExtent));
        OnPropertyChanged(nameof(CodeBlockCornerRadius));
        OnPropertyChanged(nameof(CodeBlockBackgroundShade));
        OnPropertyChanged(nameof(CodeBlockCopyButtonSize));
        OnPropertyChanged(nameof(CodeBlockCopyButtonTopOffset));
        OnPropertyChanged(nameof(CodeBlockCopyButtonRightOffset));
        OnPropertyChanged(nameof(ImagePreviewWidth));
        OnPropertyChanged(nameof(ImagePreviewHeight));
        OnPropertyChanged(nameof(CodeBlockAppearance));
    }

    private void Set(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (Math.Abs(field - value) < 0.001) return;
        field = value;
        Changed();
        OnPropertyChanged(propertyName);
    }

    private void SetCodeBlock(ref double field, double value, [CallerMemberName] string? propertyName = null)
    {
        if (Math.Abs(field - value) < 0.001) return;
        field = value;
        Changed();
        OnPropertyChanged(propertyName);
        OnPropertyChanged(nameof(CodeBlockAppearance));
    }

    private void Changed()
    {
        ValuesChanged?.Invoke(this, EventArgs.Empty);
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    internal void SaveNow() => Save();

    private void Save()
    {
        _saveTimer.Stop();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(
            new SettingsValues(
                OverallScale, TextScale, IconScale, RenderingProfile, RevealMarkdownOnHover, AutoContinueLists, Language,
                CodeBlockLeftOffset, CodeBlockRightOffset, CodeBlockTopExtent, CodeBlockBottomExtent,
                CodeBlockCornerRadius, CodeBlockBackgroundShade, CodeBlockCopyButtonSize,
                CodeBlockCopyButtonTopOffset, CodeBlockCopyButtonRightOffset,
                ImagePreviewWidth, ImagePreviewHeight),
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
        bool RevealMarkdownOnHover = false,
        bool AutoContinueLists = true,
        string? Language = null,
        double CodeBlockLeftOffset = -4,
        double CodeBlockRightOffset = -4,
        double CodeBlockTopExtent = 3,
        double CodeBlockBottomExtent = 3,
        double CodeBlockCornerRadius = 5,
        double CodeBlockBackgroundShade = 39,
        double CodeBlockCopyButtonSize = 24,
        double CodeBlockCopyButtonTopOffset = 5,
        double CodeBlockCopyButtonRightOffset = 7,
        double? ImagePreviewWidth = null,
        double? ImagePreviewHeight = null);
}
