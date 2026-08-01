# ADR 0007: Persist a resizable image preview

Date: 2026-08-01

## Status

Accepted

Supersedes ADR 0004's fixed `420 × 320` preview size.

## Context

Once standalone images render directly in the note, a `420 × 320` preview is often close to the inline image size and provides little additional value. The preview is still useful for inspecting a larger image and opening the original, but users need control over its size.

## Decision

Open image previews at `720 × 520` by default. Keep `420 × 320` as the normal minimum, shrinking further only when the current screen work area cannot fit that minimum.

Add a lower-right resize grip using the application's restrained gray visual language. Dragging the grip changes preview width and height. The preview remains centered when opened; resizing grows from its current top-left position.

Persist the last completed drag size in `UserSettings`. All note windows share that preference, and normal application exit flushes pending settings immediately.

The rendered image inside the note has no visible card background, border, or rounded frame. Only the editable Markdown image syntax receives the darker grouped background. Missing or unavailable images may retain a placeholder card because no image exists to display.

## Consequences

- The preview provides a meaningfully larger inspection surface than the inline rendering.
- Users can choose a size appropriate for their display and image content.
- The resize affordance is visible without adding a toolbar action.
- Preview dimensions become part of persisted user settings and migrate with defaults for existing settings files.
