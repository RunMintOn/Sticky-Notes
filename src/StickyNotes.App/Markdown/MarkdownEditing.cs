using System.Text.RegularExpressions;

namespace StickyNotes.App.Markdown;

public readonly record struct MarkdownEditResult(string Text, int SelectionStart, int SelectionLength);

public static partial class MarkdownEditing
{
    public static MarkdownEditResult TypeBacktick(string text, int selectionStart, int selectionLength)
    {
        if (selectionLength <= 0)
            return new MarkdownEditResult(text, selectionStart, selectionLength);

        var surrounding = 0;
        while (surrounding < 2 &&
               selectionStart > surrounding &&
               selectionStart + selectionLength + surrounding < text.Length &&
               text[selectionStart - surrounding - 1] == '`' &&
               text[selectionStart + selectionLength + surrounding] == '`')
        {
            surrounding++;
        }

        if (surrounding == 0)
            return ToggleInline(text, selectionStart, selectionLength, "`");

        if (surrounding == 1)
        {
            var expanded = text
                .Insert(selectionStart + selectionLength + 1, "`")
                .Insert(selectionStart - 1, "`");
            return new MarkdownEditResult(expanded, selectionStart + 1, selectionLength);
        }

        var selected = text.Substring(selectionStart, selectionLength);
        var fenced = "```\n" + selected + (selected.EndsWith('\n') ? "" : "\n") + "```";
        var promoted = text
            .Remove(selectionStart - 2, selectionLength + 4)
            .Insert(selectionStart - 2, fenced);
        return new MarkdownEditResult(promoted, selectionStart + 2, selectionLength);
    }

    public static MarkdownEditResult? ContinueList(string text, int caretOffset)
    {
        var lineStart = caretOffset == 0 ? -1 : text.LastIndexOf('\n', caretOffset - 1);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var lineEnd = text.IndexOf('\n', caretOffset);
        if (lineEnd < 0) lineEnd = text.Length;
        if (caretOffset != lineEnd) return null;

        var line = text[lineStart..lineEnd].TrimEnd('\r');
        var match = ContinuationPrefix().Match(line);
        if (!match.Success) return null;

        if (line.Length == match.Length)
        {
            var withoutPrefix = text.Remove(lineStart, match.Length);
            return new MarkdownEditResult(withoutPrefix, lineStart, 0);
        }

        var prefix = match.Value;
        if (match.Groups["number"].Success)
        {
            var number = int.Parse(match.Groups["number"].Value) + 1;
            prefix = match.Groups["indent"].Value + number + match.Groups["delimiter"].Value + " ";
        }
        else if (match.Groups["task"].Success)
        {
            prefix = match.Groups["indent"].Value + match.Groups["bullet"].Value + " [ ] ";
        }

        var insertion = "\n" + prefix;
        var continued = text.Insert(caretOffset, insertion);
        return new MarkdownEditResult(continued, caretOffset + insertion.Length, 0);
    }

    public static MarkdownEditResult ToggleInline(
        string text,
        int selectionStart,
        int selectionLength,
        string marker)
    {
        if (selectionLength > 0)
            return ToggleSelection(text, selectionStart, selectionLength, marker);

        var lineStart = selectionStart == 0 ? -1 : text.LastIndexOf('\n', selectionStart - 1);
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        var lineEnd = text.IndexOf('\n', selectionStart);
        if (lineEnd < 0) lineEnd = text.Length;

        var line = text[lineStart..lineEnd].TrimEnd('\r');
        var prefixLength = StructuralPrefix().Match(line).Length;
        var bodyStart = lineStart + prefixLength;
        var bodyLength = line.Length - prefixLength;
        if (bodyLength == 0) return new MarkdownEditResult(text, selectionStart, 0);

        return ToggleSelection(text, bodyStart, bodyLength, marker);
    }

    private static MarkdownEditResult ToggleSelection(
        string text,
        int start,
        int length,
        string marker)
    {
        if (start >= marker.Length && start + length + marker.Length <= text.Length &&
            text.AsSpan(start - marker.Length, marker.Length).SequenceEqual(marker) &&
            text.AsSpan(start + length, marker.Length).SequenceEqual(marker))
        {
            var result = text.Remove(start + length, marker.Length).Remove(start - marker.Length, marker.Length);
            return new MarkdownEditResult(result, start - marker.Length, length);
        }

        var selected = text.Substring(start, length);
        if (selected.StartsWith(marker, StringComparison.Ordinal) &&
            selected.EndsWith(marker, StringComparison.Ordinal) &&
            selected.Length > marker.Length * 2)
        {
            var content = selected[marker.Length..^marker.Length];
            var result = text.Remove(start, length).Insert(start, content);
            return new MarkdownEditResult(result, start, content.Length);
        }

        var wrapped = marker + selected + marker;
        var wrappedText = text.Remove(start, length).Insert(start, wrapped);
        return new MarkdownEditResult(wrappedText, start + marker.Length, length);
    }

    [GeneratedRegex(@"^[ \t]*(?:(?:#{1,6}|>)[ \t]+|(?:[-+*][ \t]+\[[ xX]\]|[-+*]|\d+[.)])[ \t]+)")]
    private static partial Regex StructuralPrefix();

    [GeneratedRegex(@"^(?<indent>[ \t]*)(?:(?<task>(?<bullet>[-+*])[ \t]+\[[ xX]\][ \t]+)|(?<number>\d+)(?<delimiter>[.)])[ \t]+|[-+*][ \t]+|>[ \t]?)")]
    private static partial Regex ContinuationPrefix();
}
