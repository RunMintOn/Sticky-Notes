# ADR 0004: Images use on-demand note-centered previews

Date: 2026-07-17

## Status

Accepted

## Context

ADR 0002 included inline image rendering in the Markdown live-preview surface. Inline images change the note's visual flow and can interrupt text-oriented writing. Narrow notes also make an inline preview too small to inspect.

The desired behavior is to preserve Markdown as visible text and treat an image as an attachment that is previewed only when requested.

## Decision

Keep the Markdown image syntax visible instead of rendering the image inline.

Replace the leading image marker visually with a small image button immediately before the rest of its Markdown link. Each image therefore keeps a local, unambiguous preview action without a permanent editor gutter.

Clicking the image button opens a `420 × 320` transient preview card:

- the card is centered on the Note Window rather than constrained to its content width;
- it may extend beyond a narrow Note Window without resizing;
- popup placement must remain inside the monitor work area;
- clicking anywhere outside the card, pressing `Esc`, or using its close button closes it;
- opening the original delegates to the operating system's default image viewer;
- unavailable or unsupported images show a non-fatal placeholder.

The preview is not a taskbar window and does not change note content, line height, or window size.

Clipboard import and image decoding are untrusted-input boundaries. Expected clipboard, file-system, URI, and image-format failures must not escape onto the UI event loop.

## Consequences

- Text remains the dominant editor surface and note layout stays stable.
- Preview remains readable when a note is narrow.
- The image button occupies the visual position of the Markdown image marker, so it adds no permanent gutter.
- Detailed zooming, rotation, and full-screen viewing remain the responsibility of the system image viewer.
- The image is decoded only when requested instead of during ordinary editor rendering.

## Supersedes

This decision supersedes only the inline image-rendering behavior described in ADR 0002. ADR 0002's Markdown storage and local attachment decisions remain in effect.
