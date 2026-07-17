using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Media;
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

        var zh = Language == "中文";
        SetResource("AppNameText", zh ? "便笺" : "Sticky Notes");
        SetResource("SearchText", zh ? "搜索…" : "Search...");
        SetResource("SettingsText", zh ? "设置" : "Settings");
        SetResource("AppearanceText", zh ? "外观" : "Appearance");
        SetResource("LayoutScaleText", zh ? "界面缩放" : "Layout scale");
        SetResource("LayoutScaleHelp", zh ? "控件高度、间距、卡片和菜单" : "Control heights, spacing, cards, and menus");
        SetResource("TextScaleText", zh ? "文字缩放" : "Text scale");
        SetResource("TextScaleHelp", zh ? "便签正文、列表、搜索和菜单文字" : "Note content, lists, search, and menu labels");
        SetResource("IconScaleText", zh ? "图标缩放" : "Icon scale");
        SetResource("IconScaleHelp", zh ? "窗口命令和格式工具" : "Title-bar commands and formatting tools");
        SetResource("TextRenderingText", zh ? "文字渲染" : "Text rendering");
        SetResource("EditingText", zh ? "编辑" : "Editing");
        SetResource("HoverMarkersText", zh ? "鼠标经过一行时显示 Markdown 标记" : "Reveal Markdown markers when the pointer hovers over a line");
        SetResource("HoverMarkersHelp", zh ? "关闭后，仅在光标所在行显示标记。" : "When disabled, markers appear only on the line containing the caret.");
        SetResource("ContinueListsText", zh ? "按回车时自动续写列表" : "Continue lists when pressing Enter");
        SetResource("ContinueListsHelp", zh ? "支持项目符号、编号列表、任务列表和引用。" : "Supports bullets, numbered lists, tasks, and block quotes.");
        SetResource("LanguageText", zh ? "语言" : "Language");
        SetResource("ResetSettingsText", zh ? "重置所有设置" : "Reset all settings");
        SetResource("NewNoteText", zh ? "新建便签" : "New note");
        SetResource("BringToFrontText", zh ? "将所有打开的便签移到前面" : "Bring all open notes to front");
        SetResource("BackText", zh ? "返回" : "Back");
        SetResource("MenuText", zh ? "菜单" : "Menu");
        SetResource("InlineCodeText", zh ? "行内代码" : "Inline code");
        SetResource("InsertImageText", zh ? "插入图片" : "Insert image");
        SetResource("NotesListText", zh ? "便签列表" : "Notes list");
        SetResource("DeleteNoteText", zh ? "删除便签" : "Delete note");
        SetResource("HelpText", zh ? "帮助与快捷键" : "Help & shortcuts");
        SetResource("CopyCodeText", zh ? "复制代码" : "Copy code");
        SetResource("CodeBlockTuningText", zh ? "代码块调试（实验性）" : "Code block tuning (experimental)");
        SetResource("CodeBlockLeftText", zh ? "左边缘偏移" : "Left edge offset");
        SetResource("CodeBlockRightText", zh ? "右边缘偏移" : "Right edge offset");
        SetResource("CodeBlockTopText", zh ? "顶部扩展" : "Top extent");
        SetResource("CodeBlockBottomText", zh ? "底部扩展" : "Bottom extent");
        SetResource("CodeBlockRadiusText", zh ? "圆角半径" : "Corner radius");
        SetResource("CodeBlockShadeText", zh ? "背景深浅" : "Background shade");
        SetResource("CopySizeText", zh ? "复制图标大小" : "Copy icon size");
        SetResource("CopyTopText", zh ? "复制图标顶部偏移" : "Copy icon top offset");
        SetResource("CopyRightText", zh ? "复制图标右侧偏移" : "Copy icon right offset");
    }

    private static void SetResource(string key, object value) =>
        Application.Current.Resources[key] = value;

    private void Save()
    {
        _saveTimer.Stop();
        File.WriteAllText(_filePath, JsonSerializer.Serialize(
            new SettingsValues(
                OverallScale, TextScale, IconScale, RenderingProfile, RevealMarkdownOnHover, AutoContinueLists, Language,
                CodeBlockLeftOffset, CodeBlockRightOffset, CodeBlockTopExtent, CodeBlockBottomExtent,
                CodeBlockCornerRadius, CodeBlockBackgroundShade, CodeBlockCopyButtonSize,
                CodeBlockCopyButtonTopOffset, CodeBlockCopyButtonRightOffset),
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
        double CodeBlockCopyButtonRightOffset = 7);

    private sealed record TextRenderingValues(
        string FontFamily,
        TextFormattingMode FormattingMode,
        TextRenderingMode RenderingMode,
        TextHintingMode HintingMode);
}
