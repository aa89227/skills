---
name: ascii-flowchart
description: |
  Create or revise compact, terminal-friendly ASCII flowcharts for processes, decisions, and
  state transitions. Use when the user asks for an ASCII or text-only flowchart, or a diagram
  that must render reliably in a monospaced interface. Do not use for Mermaid, SVG, image, or
  UI diagram work unless an ASCII version is explicitly requested.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["ascii", "flowchart", "diagram", "readability"]
---

# ASCII Flowchart

Create a diagram that a reader can trace without a visual renderer. Keep the geometry portable,
the labels concise, and the user's language in the labels.

## Constraints

- Use printable ASCII for diagram geometry: parentheses, brackets, braces, `|`, `v`, `-`, `+`,
  `>`, and spaces. Do not use Unicode arrows, box-drawing characters, emoji, tabs, or ANSI
  styling. Labels may use the user's language; if strict 7-bit ASCII is requested, keep labels
  ASCII too.
- Use one shape for each semantic role: `(Start)` and `(End)` for terminals, `[action]` for
  work, and `{condition?}` for decisions. Do not add decorative shapes that need a legend.
- Default to a top-to-bottom layout. Use left-to-right only for a short branch or when it clearly
  reduces height without making the path harder to trace.
- Use `|`, `v`, and `-->` for connectors. Label every decision exit on or immediately beside its
  connector (`yes`/`no`, `success`/`failure`, or the source's actual outcomes).
- Avoid diagonal connectors, crossings, long detours, and ambiguous line joins. Repeat a small
  node or split the flow into sub-diagrams when that is clearer than drawing a maze.
- Keep the diagram within 80 terminal columns (target 72). Keep each node to one idea, ideally
  no more than 28 visible columns per line and three lines. Wrap labels or split the diagram when
  these limits are exceeded; never solve overflow by making labels cryptic.
- Use a default complexity budget of 12 nodes (including start/end), three decisions, and two
  outgoing branches per decision. For a larger process, provide an overview and linked detail
  sub-flows. Exceed the budget only when the user explicitly requests a single complete diagram,
  and state that it is denser than the default.

## Workflow

1. Extract the confirmed start, actions, decisions, outcomes, exceptions, and any loops from the
   user's description. Do not invent missing branches or behavior.
2. Reduce each node to one short action or question. Preserve distinctions that change the route;
   omit implementation detail that does not change the flow.
3. Choose the simplest layout that keeps every path traceable. A decision with more than two
   outcomes should use explicit numbered or named exits, or be split into a sub-flow; do not hide
   outcomes under an unexplained `other` branch.
4. Render the result in a `text` fenced code block. Put the diagram first, followed only by a
   short legend or an `Assumptions` note when needed.
5. Check the rendered text before answering: there is a visible start, every non-looping path
   reaches an end, all decision exits are labeled, no node is disconnected, no connector crosses
   another path, and the width/character limits hold.

If the input is ambiguous and the ambiguity changes the topology, ask one focused question. If it
does not, draw the confirmed flow and list the assumption outside the diagram. For loops, label the
back path explicitly (`retry`, `repeat`, etc.); split the loop into a detail sub-flow if the return
connector would be hard to follow.

## Template

Use this shape as a starting point, adapting labels and branch names to the source:

```text
(Start)
   |
   v
[Receive input]
   |
   v
{Valid?} -- no --> [Show error] --> (End)
   |
  yes
   v
[Process input] --> (End)
```

Honor explicit user preferences such as orientation, symbol set, language, or a request for full
detail, while retaining unambiguous connectors and readable labels.
