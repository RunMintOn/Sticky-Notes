using System.Windows;

namespace StickyNotes.App.Tests;

public sealed class WindowPlacementTests
{
    private static readonly Rect Desktop = new(0, 0, 2560, 1600);

    [Fact]
    public void MinimizedSentinelCoordinatesAreRestoredToTheVisibleDesktop()
    {
        var restored = WindowPlacement.EnsureAccessible(
            new Rect(-21845.33, -21845.33, 570, 424),
            Desktop);

        Assert.Equal(new Rect(995, 588, 570, 424), restored);
    }

    [Fact]
    public void AccessibleWindowCoordinatesArePreserved()
    {
        var requested = new Rect(2113.33, 52.67, 834.67, 1084);

        var restored = WindowPlacement.EnsureAccessible(requested, Desktop);

        Assert.Equal(requested, restored);
    }
}
