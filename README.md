# Win Sticky Notes

A lightweight, local-first recreation of the classic Windows Sticky Notes experience.

## Current milestone

The first runnable WPF shell includes:

- one process for the notes list and all note windows;
- classic dark note/list styling based on the reference screenshots;
- single-view Markdown live preview with headings, bold, italic, strikethrough, highlights, blockquotes, links, and lists;
- inline local images and clipboard image paste using relative attachment paths;
- note colors, search, create, open, close, and delete;
- live, persisted overall/text/icon scaling from the Settings Page;
- an in-place Settings Page with optional pointer-hover Markdown marker reveal;
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

See also [`MAINTENANCE.md`](MAINTENANCE.md) for debugging, measurement
methodology, and common pitfalls.

The UI stack and memory measurements are recorded in
[`docs/decisions/0001-use-wpf.md`](docs/decisions/0001-use-wpf.md).
