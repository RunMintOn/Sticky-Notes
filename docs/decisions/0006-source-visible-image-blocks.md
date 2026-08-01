# ADR 0006: Keep image source visible above rendered attachments

Date: 2026-08-01

## Status

Accepted

Supersedes ADR 0004's icon-first, on-demand-only interaction.

## Context

The on-demand preview kept notes text-first, but required a click whenever the user wanted to see an attachment. A live-preview approach that replaces Markdown source with an image was previously rejected because moving the caret changes the layout and makes the document jump.

The desired behavior is different from either approach: the Markdown source and rendered image should both remain visible, so editing does not switch between two layouts.

## Decision

For valid Markdown image syntax that occupies a line by itself, show the rendered image directly below its source:

```markdown
![description](attachments/example.png)
```

The source remains real, editable AvalonEdit text. It receives a subtle rounded background to communicate that the syntax is one attachment reference. The image receives reserved document height and is drawn in an interactive overlay, following the existing layered-rendering precedent for code blocks.

An image reference mixed with other text does not render a block. A normal Markdown link without the leading `!` is not an image and does not render.

Remove the icon that previously replaced the leading `!`. Selecting the rendered image opens the existing fixed-size preview, which retains its open-original action.

Image decoding remains asynchronous, bounded, cached, and defensive. Missing, remote, malformed, or undecodable sources display a non-fatal unavailable placeholder.

## Consequences

- Images are visible without an extra action.
- Markdown source never disappears, so caret movement does not switch layouts.
- The leading `!` keeps its standard Markdown meaning and remains visible.
- Standalone-line rendering avoids surprising paragraph expansion for inline image syntax.
- The image rendering module owns source highlighting, reserved layout space, asynchronous loading, overlay positioning, and preview activation behind one interface.
