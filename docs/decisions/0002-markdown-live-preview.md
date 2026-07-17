# ADR 0002: Markdown is edited in a single live-preview surface

Date: 2026-07-16

## Status

Accepted

## Decision

Use Markdown as the canonical note content and present it through one native WPF live-preview editor. Do not add separate source and preview modes.

The editor uses AvalonEdit for text editing and Markdig for precise source spans. Markdown markers are collapsed on inactive lines while their semantic style is rendered. Moving the pointer over a line reveals its markers; moving the caret into a line keeps those markers visible and editable.

The initial supported surface is headings, bold, italic, strikethrough, `==highlights==`, inline code, blockquotes, links, unordered/ordered/task lists, and images. YAML, HTML, tables, mathematics, and complex code blocks receive no special rendering.

Local images are copied under the application data `attachments/<note-id>/` directory and referenced by relative Markdown paths. Clipboard image paste is supported. Remote images must not synchronously download on the editor thread and initially render as unavailable placeholders.

## Rationale

- The user frequently pastes Markdown and wants it rendered without choosing between two views.
- Markdown remains exact and portable instead of round-tripping through RTF.
- AvalonEdit preserves mature caret, selection, undo, and IME behavior.
- The native implementation avoids WebView2 memory and process overhead.

## Validation so far

A Release build rendered an inactive note with hidden heading/emphasis markers, heading sizing, bold, italic, and an inline local image. The same process remained responsive with the native editor and parser loaded.

The next interaction checkpoint is caret and hover feel under real typing, especially Chinese IME input.
