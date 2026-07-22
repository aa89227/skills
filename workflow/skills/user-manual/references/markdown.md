# Markdown Output Conventions

Use plain Markdown only — no embedded HTML, no inline CSS, no color. Markdown renderers vary too
much (terminal, editor, GitHub, plain viewer) to rely on either.

## Structure

- `#`/`##`/`###` headings for sections; keep the hierarchy shallow (3 levels max).
- Tables for structured content: enums, states, exception rows (situation → system behavior → what
  the user sees).
- A table of contents only once the document has more than ~6 sections: a plain nested list of
  links to heading anchors (GitHub-style slugs), not a styled box.

## Callouts

There is no color in plain Markdown, so signal known-gaps and corrected-claims with a bold label
inside a blockquote instead of decoration:

> **Known gap:** the "已兌換" stat always shows a placeholder — no code path sets that status yet.

> **Correction:** the create-page hint says this setting affects the claim page; verification found
> the claim-page component never reads it.

Use these two labels only (`Known gap:`, `Correction:`) — do not invent additional callout types.

## Citations

- Mark a claim with a footnote: `...same code the second time.[^1]`
- Collect them in a `## Citations` section at the end:
  ```markdown
  [^1]: `PrizeCodeCommandService.cs`, `ClaimAsync()` — idempotent early return on already-claimed status.
  ```
- Cite the exact file and function/line, never just the directory or the class name alone.

## What not to do

- Do not embed `<div>`, `<span style>`, or any raw HTML — if a renderer doesn't support it, the
  document degrades into visible tag soup.
- Do not use emoji as a substitute for the callout labels above.
- Do not hand-roll a table of contents with dot-leaders or box-drawing characters.
