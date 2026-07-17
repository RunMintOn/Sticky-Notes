# ADR 0001: Use WPF for the desktop UI

Date: 2026-07-16

## Status

Accepted

## Decision

Build the application as a single-process .NET 8 WPF application. Do not use WebView2. Reconsider native Rust/Win32 only if the production application exceeds the memory budget after profiling.

## Evidence

A throwaway Release x64 WPF spike used one manager window and one `RichTextBox` per note window, plus representative headers, formatting toolbars, list content, Chinese text, and custom window chrome.

Measurements were taken after a 10-second settling period using the Windows `Working Set - Private` performance counter:

| Scenario | Private working set | Total working set | Private commit | Idle CPU |
| --- | ---: | ---: | ---: | ---: |
| WPF manager only | 28.3 MB | 104.7 MB | 65.5 MB | — |
| WPF manager + 2 notes, run 1 | 33.1 MB | 112.5 MB | 80.0 MB | 0.0% |
| WPF manager + 2 notes, run 2 | 32.9 MB | 112.3 MB | 78.2 MB | 0.0% |
| WPF manager + 20 notes | 55.7 MB | 141.6 MB | 120.2 MB | 0.0% |
| Installed Microsoft Sticky Notes in its current state | 38.9–44.2 MB | 156.7–162.1 MB | 106.1–106.6 MB | not measured |

The installed application is UWP/XAML compiled with Microsoft .NET Native 2.2. Its Task Manager number is private working set, not total working set.

The WPF spike was already close to the installed application's footprint. A complete application will add storage and application state, but native Rust/Win32 is unlikely to save enough memory to justify its substantially higher rich-text, IME, DPI, accessibility, and window-management cost.

## Performance budget

- Manager plus two ordinary notes: target at or below 70 MB private working set.
- Idle CPU: effectively 0%.
- Opening 20 ordinary text notes: target below 150 MB private working set.
- No process per note and no browser runtime.
- Profile resource retention if closing notes does not return memory under pressure.

If manager plus two notes remains above 100 MB after profiling and reasonable optimization, build a focused Rust/Win32 comparison spike before changing stacks.

## Caveats

### Reproducing the measurements

All memory numbers use the `Working Set - Private` performance counter —
the same metric Task Manager shows by default. Do not compare using
`Process.WorkingSet64` or "total working set".

To reproduce, let the Release build settle for 8–10 seconds, then:

```powershell
$counter = Get-Counter '\Process(StickyNotes.App*)\Working Set - Private'
$privateMB = ($counter.CounterSamples |
    Where-Object InstanceName -Like 'stickynotes.app*' |
    Measure-Object CookedValue -Sum).Sum / 1MB
```

The spike did not include SQLite, inline image decoding, or production services. Startup `WaitForInputIdle` was 0.7–0.8 seconds for two notes, but that metric does not prove when every note finished rendering and is not being used as a launch-time verdict.
