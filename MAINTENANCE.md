# Maintenance Guide

## Build & Run

```powershell
dotnet build .\WinStickyNotes.sln -c Release
dotnet run --project .\src\StickyNotes.App\StickyNotes.App.csproj -c Release
```

If a previous Release build is still running, `dotnet build` will retry 10 times
then fail with `MSB3021`. Either stop the old process first, or build to a
separate output directory:

```powershell
dotnet build .\src\StickyNotes.App\StickyNotes.App.csproj -c Release -o .artifacts\debug
```

`.artifacts/` is gitignored.

---

## Debugging startup crashes

The project is a `WinExe` — unhandled XAML/resource/binding exceptions during
startup silently kill the process with no console output. To see the full
stack trace, run the DLL through the `dotnet` host:

```powershell
dotnet .\artifacts\release\StickyNotes.App.dll
```

---

## Data & Configuration

| File | Location | Content |
|---|---|---|
| `notes.json` | `%LOCALAPPDATA%\WinStickyNotes\` | Serialised note array |
| `settings.json` | `%LOCALAPPDATA%\WinStickyNotes\` | Appearance & editing preferences |
| Attachments | `…\attachments\<note-id>\` | Imported images |

Override the data root at any time via the `WIN_STICKY_NOTES_DATA_DIR`
environment variable — useful for isolated testing or pointing at a snapshot:

```powershell
$env:WIN_STICKY_NOTES_DATA_DIR = 'D:\tmp\test-notes'
dotnet run --project .\src\StickyNotes.App\StickyNotes.App.csproj -c Release
```

### Persistence details

- Content is raw Markdown in JSON. No database.
- Auto-save is debounced at 350 ms; settings save at 300 ms.
- Writes are atomic (`.tmp` → rename). On `OnExit` a synchronous save runs.
- Corrupt `notes.json` on load gets renamed to `.invalid`; an empty list is used.
- Legacy `appearance.json` is migrated automatically if `settings.json` is
  absent.

---

## DPI & Text Rendering

### DPI Awareness

The manifest (`app.manifest`) declares `PerMonitorV2`. **Without this the
window is rendered at 96 DPI then bitmap-scaled, producing fuzzy text on
non-100% displays.** The setting takes effect at process creation — changing
the manifest does nothing while the app is running.

### Rounded corners

`NativeWindowStyle.cs` uses `DwmSetWindowAttribute` (attribute 33, value 2)
for Windows 11 native corners. `WindowChrome.CornerRadius` is set to 0
because WPF's own rounding produces visible stepping at small values.

Do not replace the `DllImport` with `LibraryImport` — the source generator
would require `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>` in the project
file.

### Rendering profiles

Four profiles in Settings Page switch font family, `TextFormattingMode`,
`TextRenderingMode`, and `TextHintingMode` at runtime via dynamic resources.
Default: *Original-like ClearType* (`Display` + `ClearType` + `Fixed` +
`Microsoft YaHei UI`).

---



## Retrieving papercuts for this project

Papercuts are logged to `~/.pi/papercuts.md` with an optional `**Path:**`
header. To retrieve only entries matching the current project:

```powershell
.\tools\find-papercuts.ps1
```

This script filters entries whose path matches the repository root and
ignores papercuts from other projects.

---

## Known Pitfalls

| Symptom | Cause | Fix |
|---|---|---|
| XAML error "Data at the root level is invalid" | Duplicate BOM (`\uFEFF`) in `.xaml` | Strip leading BOM |
| `LibraryImport` gives `CS0227: Unsafe code required` | Source-generated P/Invoke | Use `DllImport` (see `NativeWindowStyle.cs`) |
| AvalonEdit markers don't hide on inactive lines | Marker generator returns wrong offset | `_isLineRevealed` must check the block's start offset |
| `GetProcessDpiAwareness` returns `SystemAware` despite manifest | Manifest not linked | Add `<ApplicationManifest>app.manifest</ApplicationManifest>` to `.csproj` |
| `WindowDpi == 96` on a 125% display | Window created on 96-DPI monitor then moved | `PerMonitorV2` auto-updates; `PerMonitor` does not |
| App exits silently on startup | `WinExe` + XAML load exception | Run via `dotnet .\StickyNotes.App.dll` |
| UI test captures wrong content | `CopyFromScreen` is not window-specific | Use `PrintWindow` + `UIAutomationClient` |
