# HTML Output Conventions

A single, self-contained `.html` file — everything inline, nothing fetched from a network. It is
opened directly in a browser from disk; treat it as a small, disciplined design system, not a wall
of text with headings tacked on.

## File shape

One file: `<style>` in `<head>`, content in `<body>`. No external stylesheets, fonts, images, icon
libraries, or scripts, and no CDN links — a file opened offline must render exactly the same as one
opened online.

## Typography and layout

- Font stack must cover the document's actual language, not just Latin script. For Traditional
  Chinese: `-apple-system, "PingFang TC", "Noto Sans TC", "Microsoft JhengHei", sans-serif`. Swap
  the regional fallbacks for whatever the target language needs; never ship a Latin-only stack for
  a non-Latin document.
- Constrain content width (`max-width: 880–960px`, centered) — a manual read top-to-bottom should
  not stretch edge-to-edge on a wide monitor.
- Line-height 1.6–1.8 for body text; tighter inside table cells.
- Heading hierarchy: `h1` for the document title, `h2` per top-level section with a small visual
  marker (e.g. a left accent border) so sections stay scannable, `h3`/`h4` for subsections. The
  weight difference between levels should be obvious without relying on color.

## A small, disciplined palette

Define 5–8 CSS custom properties in `:root` and use nothing else:

```css
:root {
  --primary: #1a1a2e;      /* headings, primary text */
  --accent: #06c755;       /* section markers, links, positive state */
  --border: #e2e8f0;       /* all borders/dividers */
  --bg-soft: #f7f8fa;      /* subtle section backgrounds, table headers */
  --warn-bg: #fff8e1; --warn-border: #f0c14b;     /* known-gap callouts */
  --danger-bg: #fdecea; --danger-border: #f5b3ac; /* corrected-claim callouts */
}
```

Color is reserved for exactly two callout categories, matching the two used in the Markdown
convention (`<skill-directory>/references/markdown.md`) so a reader who sees both formats learns one
taxonomy — plus small state badges (e.g. active / expired / archived pills). Do not color ordinary
prose, tables, or section backgrounds beyond the subtle `--bg-soft` — if everything has color, none
of it signals anything.

## Reusable components

Build these once as CSS classes and reuse them everywhere rather than inventing a new visual
treatment per section:

- **`.card`** — bordered, rounded container for describing one screen/page (border + radius +
  padding, no fill color).
- **`.known-gap`** (`--warn-bg`/`--warn-border`) — known-gaps and limitations callout. Matches the
  Markdown convention's known-gap category.
- **`.correction`** (`--danger-bg`/`--danger-border`) — a claim (UI copy, comment, prior assumption)
  that verification disproved. Matches the Markdown convention's correction category. Keep it
  visually distinct from `.known-gap` so a reader can tell "this is missing" apart from "this is
  actively wrong" at a glance.
- **`.badge`** — small rounded-pill label for enum-like states (active/expired/archived,
  unassigned/assigned/claimed/redeemed), one background/text color pair per state.
- **`.step`** + a centered arrow/divider between consecutive steps — for describing a sequential
  flow as numbered stages. If verification found more than one path through a flow, give each path
  its own labeled run of steps (e.g. "Path A" / "Path B") instead of forcing one linear sequence —
  see the parent skill's writing rules on multi-path flows.
- **`.cite`** — small, muted text for inline source captions (e.g. the internal enum name shown
  under a plain-language label) and citation summary lines.

The class names above (`.known-gap`, `.correction`) are internal identifiers — keep them in English
regardless of the document's language, exactly like a variable name. The `<span class="label">`
text inside each box is reader-facing and must be written in the document's language. In a
Traditional Chinese document:

```html
<div class="known-gap"><span class="label">已知限制</span><br>...</div>
<div class="correction"><span class="label">更正</span><br>...</div>
```

Never leave the label itself in English ("Known gap", "Correction") inside an otherwise-translated
document — that is the one place this convention is most tempting to get lazy about, because the
class name and the label look like they should match.

## Table of contents

Once the document has more than ~4 top-level sections, add a linked table of contents right after
the scope note: a plain nested `<ul>` of `<a href="#section-id">` links, styled as a quiet box
(`--bg-soft` background, no border needed). Give every `<h2>`/`<h3>` a matching `id`.

## Citations

- Mark a claim with `<sup class="fn">[n]</sup>`, styled as small, colored, bold text — visually
  distinct from body text but not distracting.
- Collect every citation in one `<table>` appendix at the end: columns `#`, claim, source
  (`file:line` or exact function). One row per citation, in numeric order, no gaps.
- Never put a raw file path, function name, or status code in body text — only in this table.

## Theme

A single light theme is enough for a local file opened directly from disk. Do not build a
`prefers-color-scheme` dark variant or a JS theme toggle unless the destination environment already
provides one and expects it. Keep the file static — no client-side scripting for a document whose
only job is to be read.

## What not to do

- Do not reach for a CSS framework or icon font "for polish" — the whole point is zero external
  dependencies.
- Do not add interactivity (search box, collapsible sections, tabs) unless the user explicitly asks
  for it — a manual is read top to bottom, not operated.
- Do not vary the color palette per section for visual interest; reuse the same 5–8 tokens
  everywhere.
- Do not skip the table of contents on a long document just because it's a single file — the reader
  still needs to navigate it.
