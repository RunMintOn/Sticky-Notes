# ADR 0003: Three-layer rendering for fenced code blocks

**Date:** 2026-07-18

## Status

Accepted

## Context

Fenced code blocks need a full-width contiguous dark background that extends outside the text area's left edge. AvalonEdit's `IBackgroundRenderer` draws at the `TextView` layer, which clips to the text area — the background could never reach the left gutter or extend past the text margin.

Additionally, the copy button must be positioned at the code block's top-right and track scrolling, requiring an interactive overlay that does not interfere with AvalonEdit's caret, selection, or IME.

## Decision

Render code blocks across three independent layers in `MarkdownEditor`:

| Layer | Element | Content | Interactive |
|---|---|---|---|
| 1 | `CodeBlockBackground` (Canvas) | Rounded dark rectangle per code block | No |
| 2 | `TextEditor` (AvalonEdit) | Raw Markdown with monospace styling for code fences and content | Yes (editing) |
| 3 | `CodeBlockOverlay` (Canvas) | Copy button per code block | Yes (click) |

Layer 1 and 3 are sibling canvases in the same Grid as the TextEditor. Positions are calculated by translating the `TextView`'s visual line geometry into each canvas's coordinate space at render time and after every `VisualLinesChanged` / `ScrollOffsetChanged` event.

The `IBackgroundRenderer` approach that was used previously for this purpose has been removed — it cannot escape the `TextView` clipping region.

## Consequences

- The background can extend freely in any direction (previously clipped at the text left edge).
- The layers are explicit and testable independently of AvalonEdit's rendering pipeline.
- Overlay position must be manually recalculated on scroll and visual-line changes; the `ScheduleCodeBlockOverlay` method debounces this to `DispatcherPriority.Render`.
- The rendering is not CSS-like; extending the appearance (e.g. line numbers, collapse controls) requires adding items to layer 1 or 3.
