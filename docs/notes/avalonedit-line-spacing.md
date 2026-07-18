# AvalonEdit line spacing

## Status

Deferred. The current spacing is acceptable.

## Goal

Add one global **Line spacing** setting for all visual lines. Do not maintain separate spacing rules for headings, lists, quotes, or code blocks.

## Why it is deferred

AvalonEdit calculates line height internally. `TextView.DefaultLineHeight` is read-only, and setting WPF's attached `TextBlock.LineHeight` does not change it (verified locally: `15.24 → 15.24`).

Avoid visual translation, inserted blank lines, or Markdown-specific margins: they can desynchronise text from the caret/selection, affect IME positioning, or alter the Markdown source.

## Starting points

Project integration:

- `src/StickyNotes.App/Markdown/MarkdownEditor.xaml`
- `src/StickyNotes.App/Markdown/MarkdownEditor.xaml.cs`
- `src/StickyNotes.App/Services/UserSettings.cs`
- `src/StickyNotes.App/MainWindow.xaml`

AvalonEdit:

- `ICSharpCode.AvalonEdit.Rendering.TextView`
- `TextView.DefaultLineHeight`
- internal paragraph creation/layout around `CreateParagraphProperties`
- NuGet package: `%USERPROFILE%\.nuget\packages\avalonedit\6.3.1.120\`

The preferred implementation is a minimal, documented AvalonEdit fork/patch that introduces a line-spacing multiplier at paragraph layout time.

## Validation before adoption

Check caret and selection alignment, Chinese IME candidate placement, wrapped lines, scrolling, headings, lists, and fenced code blocks.
