using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using System.Text.RegularExpressions;

namespace StickyNotes.App.Markdown;

internal enum MarkdownStyleKind
{
    Bold,
    Italic,
    Strikethrough,
    Heading1,
    Heading2,
    Heading3,
    Link,
    InlineCode,
    Blockquote,
    Highlight,
    CodeBlock,
    CodeFence
}

internal readonly record struct MarkdownStyleSpan(int Start, int Length, MarkdownStyleKind Kind)
{
    internal int End => Start + Length;
}

internal readonly record struct MarkdownMarkerSpan(int Start, int Length)
{
    internal int End => Start + Length;
}

internal readonly record struct MarkdownImageSpan(
    int Start,
    int Length,
    string Url,
    string AltText,
    bool IsStandalone);
internal readonly record struct MarkdownListSpan(int Start, int Length, string DisplayText);
public readonly record struct MarkdownRuleSpan(int Start, int Length);
public readonly record struct MarkdownCodeBlockSpan(
    int Start,
    int Length,
    int BlockStart,
    int BlockLength);

public sealed class MarkdownPresentation
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UsePreciseSourceLocation()
        .UseEmphasisExtras()
        .Build();

    internal IReadOnlyList<MarkdownStyleSpan> Styles { get; private init; } = [];
    internal IReadOnlyList<MarkdownMarkerSpan> Markers { get; private init; } = [];
    internal IReadOnlyList<MarkdownImageSpan> Images { get; private init; } = [];
    internal IReadOnlyList<MarkdownListSpan> Lists { get; private init; } = [];
    public IReadOnlyList<MarkdownRuleSpan> Rules { get; private init; } = [];
    public IReadOnlyList<MarkdownCodeBlockSpan> CodeBlocks { get; private init; } = [];

    public static MarkdownPresentation Parse(string text)
    {
        if (string.IsNullOrEmpty(text)) return new MarkdownPresentation();

        var styles = new List<MarkdownStyleSpan>();
        var markers = new List<MarkdownMarkerSpan>();
        var images = new List<MarkdownImageSpan>();
        var lists = ParseLinePrefixes(text, styles);
        var document = Markdig.Markdown.Parse(text, Pipeline);
        var rules = Regex.Matches(text, @"(?m)^[ \t]{0,3}(?:-[ \t]*){3,}(?=\r?$)")
            .Select(match => new MarkdownRuleSpan(match.Index, match.Length))
            .ToArray();
        var codeBlocks = new List<MarkdownCodeBlockSpan>();
        foreach (Match match in Regex.Matches(
                     text,
                     @"(?m)^(?<open>[ ]{0,3}`{3,}[^\r\n]*\r?\n)(?<content>[\s\S]*?)(?<close>^[ ]{0,3}`{3,}[ \t]*(?=\r?$))"))
        {
            var content = match.Groups["content"];
            var contentLength = content.Length;
            while (contentLength > 0 && (text[content.Index + contentLength - 1] is '\r' or '\n')) contentLength--;
            if (contentLength > 0)
            {
                codeBlocks.Add(new MarkdownCodeBlockSpan(
                    content.Index,
                    contentLength,
                    match.Index,
                    match.Length));
                styles.Add(new MarkdownStyleSpan(content.Index, contentLength, MarkdownStyleKind.CodeBlock));
            }
            var opening = match.Groups["open"];
            var openingLength = opening.Length;
            while (openingLength > 0 && (text[opening.Index + openingLength - 1] is '\r' or '\n')) openingLength--;
            styles.Add(new MarkdownStyleSpan(opening.Index, openingLength, MarkdownStyleKind.CodeFence));
            var closing = match.Groups["close"];
            styles.Add(new MarkdownStyleSpan(closing.Index, closing.Length, MarkdownStyleKind.CodeFence));
        }

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            if (heading.IsSetext || heading.Span.Start < 0) continue;
            var prefixLength = 0;
            while (heading.Span.Start + prefixLength < text.Length &&
                   text[heading.Span.Start + prefixLength] == '#') prefixLength++;
            if (heading.Span.Start + prefixLength < text.Length &&
                text[heading.Span.Start + prefixLength] == ' ') prefixLength++;
            if (prefixLength == 0) continue;

            markers.Add(new MarkdownMarkerSpan(heading.Span.Start, prefixLength));
            var contentStart = heading.Span.Start + prefixLength;
            var contentLength = Math.Max(0, heading.Span.End + 1 - contentStart);
            var kind = heading.Level switch
            {
                1 => MarkdownStyleKind.Heading1,
                2 => MarkdownStyleKind.Heading2,
                _ => MarkdownStyleKind.Heading3
            };
            if (contentLength > 0) styles.Add(new MarkdownStyleSpan(contentStart, contentLength, kind));
        }

        foreach (var emphasis in document.Descendants<EmphasisInline>())
        {
            var count = emphasis.DelimiterCount;
            if (count <= 0 || emphasis.Span.Start < 0 || emphasis.Span.End >= text.Length) continue;
            var fullLength = emphasis.Span.End - emphasis.Span.Start + 1;
            if (fullLength <= count * 2) continue;

            markers.Add(new MarkdownMarkerSpan(emphasis.Span.Start, count));
            markers.Add(new MarkdownMarkerSpan(emphasis.Span.End - count + 1, count));
            var kind = emphasis.DelimiterChar switch
            {
                '~' => MarkdownStyleKind.Strikethrough,
                '=' => MarkdownStyleKind.Highlight,
                _ when count >= 2 => MarkdownStyleKind.Bold,
                _ => MarkdownStyleKind.Italic
            };
            styles.Add(new MarkdownStyleSpan(
                emphasis.Span.Start + count,
                fullLength - count * 2,
                kind));
        }

        foreach (var link in document.Descendants<LinkInline>().Where(link => !link.IsImage))
        {
            if (link.Span.Start < 0 || link.Span.End >= text.Length || link.LabelSpan.Start < 0) continue;
            var labelStart = link.LabelSpan.Start;
            var labelLength = link.LabelSpan.Length;
            if (labelLength <= 0) continue;

            var openingLength = Math.Max(0, labelStart - link.Span.Start);
            var suffixStart = labelStart + labelLength;
            var suffixLength = Math.Max(0, link.Span.End + 1 - suffixStart);
            if (openingLength > 0) markers.Add(new MarkdownMarkerSpan(link.Span.Start, openingLength));
            if (suffixLength > 0) markers.Add(new MarkdownMarkerSpan(suffixStart, suffixLength));
            styles.Add(new MarkdownStyleSpan(labelStart, labelLength, MarkdownStyleKind.Link));
        }

        foreach (var code in document.Descendants<CodeInline>())
        {
            if (code.Span.Start < 0 || code.Span.End >= text.Length) continue;
            var delimiterLength = 0;
            while (code.Span.Start + delimiterLength < text.Length &&
                   text[code.Span.Start + delimiterLength] == '`') delimiterLength++;
            var fullLength = code.Span.Length;
            if (delimiterLength == 0 || fullLength <= delimiterLength * 2) continue;
            markers.Add(new MarkdownMarkerSpan(code.Span.Start, delimiterLength));
            markers.Add(new MarkdownMarkerSpan(code.Span.End - delimiterLength + 1, delimiterLength));
            styles.Add(new MarkdownStyleSpan(
                code.Span.Start + delimiterLength,
                fullLength - delimiterLength * 2,
                MarkdownStyleKind.InlineCode));
        }

        foreach (var image in document.Descendants<LinkInline>().Where(link => link.IsImage))
        {
            if (image.Span.Start < 0 || image.Span.End >= text.Length || string.IsNullOrWhiteSpace(image.Url)) continue;
            images.Add(new MarkdownImageSpan(
                image.Span.Start,
                image.Span.Length,
                image.Url!,
                image.Label ?? "image",
                IsStandaloneLine(text, image.Span.Start, image.Span.Length)));
        }

        return new MarkdownPresentation
        {
            Styles = styles.OrderBy(span => span.Start).ToArray(),
            Markers = MergeMarkers(markers, text.Length),
            Images = images.OrderBy(image => image.Start).ToArray(),
            Lists = lists,
            Rules = rules,
            CodeBlocks = codeBlocks
        };
    }

    private static bool IsStandaloneLine(string text, int start, int length)
    {
        var lineStart = text.LastIndexOf('\n', Math.Max(0, start - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var lineEnd = text.IndexOf('\n', start + length);
        if (lineEnd < 0) lineEnd = text.Length;

        return string.IsNullOrWhiteSpace(text[lineStart..start]) &&
               string.IsNullOrWhiteSpace(text[(start + length)..lineEnd]);
    }

    private static IReadOnlyList<MarkdownListSpan> ParseLinePrefixes(
        string text,
        ICollection<MarkdownStyleSpan> styles)
    {
        var result = new List<MarkdownListSpan>();
        var offset = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            var line = rawLine.EndsWith('\r') ? rawLine[..^1] : rawLine;
            var quote = Regex.Match(line, @"^(?<indent>[ \t]*)>[ \t]?");
            if (quote.Success)
            {
                var indentLength = quote.Groups["indent"].Length;
                result.Add(new MarkdownListSpan(offset + indentLength, quote.Length - indentLength, "│  "));
                var contentStart = offset + quote.Length;
                if (contentStart < offset + line.Length)
                    styles.Add(new MarkdownStyleSpan(
                        contentStart,
                        offset + line.Length - contentStart,
                        MarkdownStyleKind.Blockquote));
                offset += rawLine.Length + 1;
                continue;
            }

            var task = Regex.Match(line, @"^(?<indent>[ \t]*)[-+*][ \t]+\[(?<state>[ xX])\][ \t]+");
            if (task.Success)
            {
                var indentLength = task.Groups["indent"].Length;
                var display = task.Groups["state"].Value == " " ? "☐  " : "☑  ";
                result.Add(new MarkdownListSpan(offset + indentLength, task.Length - indentLength, display));
                offset += rawLine.Length + 1;
                continue;
            }

            var unordered = Regex.Match(line, @"^(?<indent>[ \t]*)[-+*][ \t]+");
            if (unordered.Success)
            {
                var indentLength = unordered.Groups["indent"].Length;
                result.Add(new MarkdownListSpan(offset + indentLength, unordered.Length - indentLength, "•  "));
                offset += rawLine.Length + 1;
                continue;
            }

            var ordered = Regex.Match(line, @"^(?<indent>[ \t]*)(?<number>\d+)[.)][ \t]+");
            if (ordered.Success)
            {
                var indentLength = ordered.Groups["indent"].Length;
                result.Add(new MarkdownListSpan(
                    offset + indentLength,
                    ordered.Length - indentLength,
                    ordered.Groups["number"].Value + ".  "));
            }
            offset += rawLine.Length + 1;
        }
        return result;
    }

    private static IReadOnlyList<MarkdownMarkerSpan> MergeMarkers(
        IEnumerable<MarkdownMarkerSpan> source,
        int documentLength)
    {
        var ordered = source
            .Where(span => span.Start >= 0 && span.Length > 0 && span.End <= documentLength)
            .OrderBy(span => span.Start)
            .ThenBy(span => span.Length)
            .ToArray();
        if (ordered.Length == 0) return [];

        var result = new List<MarkdownMarkerSpan>();
        var current = ordered[0];
        foreach (var next in ordered.Skip(1))
        {
            if (next.Start <= current.End)
            {
                current = new MarkdownMarkerSpan(current.Start, Math.Max(current.End, next.End) - current.Start);
            }
            else
            {
                result.Add(current);
                current = next;
            }
        }
        result.Add(current);
        return result;
    }
}
