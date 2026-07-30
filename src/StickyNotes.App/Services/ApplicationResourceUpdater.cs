using System.Windows;
using System.Windows.Media;

namespace StickyNotes.App.Services;

internal static class ApplicationResourceUpdater
{
    private const string LanguageResourcePrefix = "/StickyNotes.App;component/Resources/Strings.";

    internal static void Apply(UserSettings settings)
    {
        ApplyAppearance(settings);
        ApplyLanguage(settings.Language);
    }

    private static void ApplyAppearance(UserSettings settings)
    {
        Set("TitleFontSize", 27 * settings.TextScale);
        Set("ListFontSize", 18 * settings.TextScale);
        Set("EditorFontSize", 18 * settings.TextScale);
        Set("MenuFontSize", 17 * settings.TextScale);
        Set("ChromeIconSize", 16 * settings.IconScale);
        Set("ToolbarIconSize", 18 * settings.IconScale);
        Set("HeaderHeight", 40 * settings.OverallScale);
        Set("ToolbarHeight", 49 * settings.OverallScale);
        Set("HeaderGridLength", new GridLength(40 * settings.OverallScale));
        Set("ToolbarGridLength", new GridLength(49 * settings.OverallScale));
        Set("ChromeButtonExtent", 45 * settings.OverallScale);
        Set("FormatButtonWidth", 52 * settings.OverallScale);
        Set("FormatButtonHeight", 48 * settings.OverallScale);
        Set("NoteMenuWidth", 365 * settings.OverallScale);
        Set("NoteMenuPaletteHeight", 54 * settings.OverallScale);
        Set("NoteMenuRowHeight", 62 * settings.OverallScale);
        Set("NoteCardHeight", 174 * settings.OverallScale);
        Set("NoteCardMinHeight", 105 * settings.OverallScale);

        var rendering = settings.RenderingProfile switch
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
        Set("InterfaceFontFamily", new FontFamily(rendering.FontFamily));
        Set("EditorFontFamily", new FontFamily(rendering.FontFamily));
        Set("TextFormattingMode", rendering.FormattingMode);
        Set("TextRenderingMode", rendering.RenderingMode);
        Set("TextHintingMode", rendering.HintingMode);
    }

    private static void ApplyLanguage(string language)
    {
        var source = new Uri(
            $"{LanguageResourcePrefix}{(language == "中文" ? "zh-CN" : "en")}.xaml",
            UriKind.Relative);
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var current = dictionaries.FirstOrDefault(dictionary =>
            dictionary.Source?.OriginalString.StartsWith(LanguageResourcePrefix, StringComparison.Ordinal) == true);
        if (current?.Source == source) return;
        if (current is not null) dictionaries.Remove(current);
        dictionaries.Add(new ResourceDictionary { Source = source });
    }

    private static void Set(string key, object value) => Application.Current.Resources[key] = value;

    private sealed record TextRenderingValues(
        string FontFamily,
        TextFormattingMode FormattingMode,
        TextRenderingMode RenderingMode,
        TextHintingMode HintingMode);
}
