# Win Sticky Notes

A lightweight, local-first recreation of the classic Windows Sticky Notes experience.

## Current milestone

The first runnable WPF shell includes:

- one process for the notes list and all note windows;
- classic dark note/list styling based on the reference screenshots;
- single-view Markdown live preview with headings, bold, italic, strikethrough, highlights, blockquotes, links, lists, horizontal rules, and fenced code blocks;
- inline local images and clipboard image paste using relative attachment paths;
- note colors, search, create, open, close, and delete;
- live, persisted overall/text/icon scaling from the Settings Page;
- an in-place bilingual Settings Page with marker-reveal and automatic-list-continuation preferences;
- smart `Ctrl+B`, `Ctrl+H`, and `Ctrl+D` formatting for selections or the current line;
- full-width fenced-code backgrounds with a copy command and live appearance tuning;
- selection-aware backtick typing that promotes one/two inline delimiters into a fenced block on the third backtick;
- a bilingual in-place Help Page available from `?`, the Note Menu, or `F1`;
- a command that brings all open Note Windows in front of other ordinary windows;
- content-sized Note Cards with an Open Fold for currently open notes;
- debounced local persistence of Markdown content and window bounds;
- active and inactive note chrome states.

Remote image loading and production hardening are not implemented yet.

## Run

```powershell
dotnet run --project .\src\StickyNotes.App\StickyNotes.App.csproj -c Release
```

Data is stored in `%LOCALAPPDATA%\WinStickyNotes\notes.json`.

## Build

```powershell
dotnet build .\WinStickyNotes.sln -c Release
```

## Test

```powershell
dotnet test .\tests\StickyNotes.App.Tests\StickyNotes.App.Tests.csproj -c Release
```

See also [`MAINTENANCE.md`](MAINTENANCE.md) for debugging, measurement
methodology, and common pitfalls.

The UI stack and memory measurements are recorded in
[`docs/decisions/0001-use-wpf.md`](docs/decisions/0001-use-wpf.md).
