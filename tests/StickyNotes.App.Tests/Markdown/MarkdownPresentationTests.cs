using StickyNotes.App.Markdown;

namespace StickyNotes.App.Tests.Markdown;

public sealed class MarkdownPresentationTests
{
    [Fact]
    public void ThreeHyphensProduceAHorizontalRule()
    {
        var presentation = MarkdownPresentation.Parse("before\n---\nafter");

        var rule = Assert.Single(presentation.Rules);
        Assert.Equal(7, rule.Start);
        Assert.Equal(3, rule.Length);
    }

    [Fact]
    public void FencedCodeExposesItsContentAsACodeBlock()
    {
        var presentation = MarkdownPresentation.Parse("```csharp\nvar x = 1;\n```");

        var code = Assert.Single(presentation.CodeBlocks);
        Assert.Equal(10, code.Start);
        Assert.Equal(10, code.Length);
        Assert.Equal(0, code.BlockStart);
        Assert.Equal(24, code.BlockLength);
    }
}
