# ADR 0005: Separate settings resources and secondary pages

Date: 2026-07-31

## Status

Accepted

## Context

`UserSettings` had four reasons to change: setting values and validation, JSON persistence, WPF appearance resources, and localized UI text. `MainWindow.xaml` also contained the Notes List, Settings Page, and Help Page in one file.

The files were not too large by line count alone, but these mixed responsibilities reduced locality: changing a translation required editing persistence-oriented code, and changing a secondary page required navigating the Notes List shell.

## Decision

Keep `UserSettings` responsible for setting values, validation, migration, and persistence. It exposes one aggregate `ValuesChanged` event in addition to property-level notifications.

Move WPF application resource updates behind the internal `ApplicationResourceUpdater` module. `App` owns the connection between `UserSettings.ValuesChanged` and this module.

Move localized strings into paired WPF resource dictionaries:

- `Resources/Strings.en.xaml`
- `Resources/Strings.zh-CN.xaml`

The updater swaps the active dictionary when the language changes. Appearance scaling and text rendering remain centralized in the same updater.

Extract Settings Page and Help Page into their own UserControls. Each receives one page-specific DataContext and exposes only `BackRequested` to the Notes List shell.

## Consequences

- Adding or reviewing translations no longer requires editing settings persistence code.
- `UserSettings` no longer depends directly on `Application.Current` or WPF rendering types.
- `MainWindow.xaml` is the Notes List shell rather than the owner of all secondary-page markup.
- Settings and Help can evolve independently while navigation remains owned by `MainWindow`.
- No dependency-injection framework or speculative interface hierarchy is introduced.

## Deferred

`MarkdownEditor` and `MarkdownPresentation` remain intact. They are relatively large but cohesive and already delegate rendering details. Further extraction should wait for a second concrete change that needs a new seam.
