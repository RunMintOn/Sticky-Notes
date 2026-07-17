using StickyNotes.App.Markdown;

namespace StickyNotes.App.Tests.Markdown;

public sealed class MarkdownEditingTests
{
    [Fact]
    public void BoldWithoutSelectionFormatsCurrentListItemText()
    {
        var result = MarkdownEditing.ToggleInline("before\n- list item\nafter", 12, 0, "**");

        Assert.Equal("before\n- **list item**\nafter", result.Text);
        Assert.Equal(11, result.SelectionStart);
        Assert.Equal(9, result.SelectionLength);
    }


    [Fact]
    public void EnterContinuesAnUnorderedList()
    {
        var result = MarkdownEditing.ContinueList("- first", 7);

        Assert.NotNull(result);
        Assert.Equal("- first\n- ", result.Value.Text);
        Assert.Equal(10, result.Value.SelectionStart);
    }

    [Fact]
    public void EnterOnAnEmptyTaskListItemExitsTheList()
    {
        var result = MarkdownEditing.ContinueList("- [ ] ", 6);

        Assert.NotNull(result);
        Assert.Equal("", result.Value.Text);
        Assert.Equal(0, result.Value.SelectionStart);
    }

    [Theory]
    [InlineData("plain", 0, 5, "==", "==plain==")]
    [InlineData("**plain**", 2, 5, "**", "plain")]
    [InlineData("~~plain~~", 0, 9, "~~", "plain")]
    public void InlineCommandsToggleSelectedText(
        string text, int start, int length, string marker, string expected)
    {
        var result = MarkdownEditing.ToggleInline(text, start, length, marker);

        Assert.Equal(expected, result.Text);
    }

    [Theory]
    [InlineData("3. third", 8, "3. third\n4. ")]
    [InlineData("- [x] done", 10, "- [x] done\n- [ ] ")]
    [InlineData("> quote", 7, "> quote\n> ")]
    public void EnterContinuesSupportedListKinds(string text, int caret, string expected)
    {
        var result = MarkdownEditing.ContinueList(text, caret);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value.Text);
    }

    [Fact]
    public void ThreeBackticksAroundASelectionPromoteItToAFencedCodeBlock()
    {
        var first = MarkdownEditing.TypeBacktick("line one\nline two", 0, 17);
        var second = MarkdownEditing.TypeBacktick(first.Text, first.SelectionStart, first.SelectionLength);
        var third = MarkdownEditing.TypeBacktick(second.Text, second.SelectionStart, second.SelectionLength);

        Assert.Equal("```\nline one\nline two\n```", third.Text);
        Assert.Equal(4, third.SelectionStart);
        Assert.Equal(17, third.SelectionLength);
    }
}
