---
name: ascii-flowchart
description: |
  Create or revise compact, terminal-friendly ASCII diagrams of any kind, including flowcharts,
  architecture and dependency maps, state and sequence diagrams, trees, timelines, and other
  text-only diagrams. Use when the user asks for an ASCII diagram or a diagram that must render
  reliably in a monospaced interface. Do not use for Mermaid, SVG, image, or UI diagram work
  unless an ASCII version is explicitly requested.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["ascii", "diagram", "flowchart", "architecture", "dependency", "sequence", "tree"]
---

# ASCII Diagrams

Create a diagram that a reader can trace or inspect without a visual renderer. First identify
what kind of diagram the request needs—process flow, architecture/dependency map, state or
sequence diagram, hierarchy/tree, timeline, or another ASCII diagram. Keep the geometry portable
and the labels concise. Do not apply process-flow rules to diagrams that are not process flows.

## Language and labels

- Use the language and writing system of the user's request for every human-readable label. If
  the user writes Traditional Chinese, use Traditional Chinese; do not translate labels to
  English or switch scripts unless the user asks for it.
- Prefer the user's own wording and domain terminology. Preserve supplied product names, API
  names, class names, and other identifiers in their original form unless the user asks for
  translation or localization.
- Apply the language rule to node labels, layer names, connector annotations, legends, and
  assumptions—not only to the title or explanatory prose.
- For a mixed-language request, use the dominant natural language while preserving exact
  technical identifiers and explicitly supplied labels.

## Diagram type

- For a process flow, show actions, actual decisions, outcomes, exceptions, and loops. Use
  terminal/action/decision shapes only when those concepts exist.
- For an architecture or dependency diagram, show components, boundaries, layers, ownership,
  and actual relationships. Start/end nodes and decision branches are optional and normally do
  not belong there. Do not invent branches merely to make the diagram look like a flowchart;
  natural fan-in or fan-out is fine when it represents a real relationship.
- For state or sequence diagrams, show only the transitions or interactions present in the
  source. Use labels that describe the source's states and events rather than generic English
  placeholders.
- For hierarchy, tree, timeline, relationship, or other ASCII diagrams, choose a layout that
  matches the actual parent-child, ordered, or related structure. Do not add start/end nodes or
  decision branches unless the source contains those concepts.

## Constraints

- Use printable ASCII for diagram geometry: parentheses, brackets, braces, `|`, `v`, `-`, `+`,
  `>`, and spaces. Do not use Unicode arrows, box-drawing characters, emoji, tabs, or ANSI
  styling. Labels may use the user's language; if strict 7-bit ASCII is requested, keep labels
  ASCII too. Count visible terminal columns rather than bytes; account for wide characters when
  checking a diagram containing CJK labels.
- In process flows, use parentheses for actual terminals, brackets for actions, and braces for
  actual decisions. The words inside those shapes must follow the user's language. In
  architecture diagrams, use brackets or simple ASCII grouping for components and boundaries;
  do not force terminal or decision shapes onto components.
- Default to a top-to-bottom layout for a process flow. For architecture diagrams, organize by
  layers, boundaries, or ownership and choose the orientation that makes dependencies easiest
  to follow. Use left-to-right when it is clearer, even if there is no branch.
- Use `|`, `v`, and `-->` for connectors. Label exits only when there is an actual decision or
  another relationship whose meaning is ambiguous. For architecture diagrams, state the arrow
  meaning (for example, dependency direction) when it is not obvious.
- Avoid diagonal connectors, crossings, long detours, and ambiguous line joins. Repeat a small
  node or split the flow into sub-diagrams when that is clearer than drawing a maze.
- Keep the diagram within 80 terminal columns (target 72). Keep each node to one idea, ideally
  no more than 28 visible columns per line and three lines. Wrap labels or split the diagram when
  these limits are exceeded; never solve overflow by making labels cryptic.
- For process flows, use a default complexity budget of 12 nodes (including terminals), three
  decisions, and two outgoing branches per decision. For a larger process, provide an overview
  and linked detail sub-flows. Exceed the budget only when the user explicitly requests a single
  complete diagram, and state that it is denser than the default.
- Do not impose a branch or decision budget on architecture diagrams. Keep only the components
  relevant to the requested scope; if the dependency graph is dense, provide an overview and
  focused detail diagrams instead of deleting real relationships or inventing branches.

## Workflow

1. Identify the diagram type and extract only confirmed elements. For a process, collect
   actions, decisions, outcomes, exceptions, and loops. For architecture, collect components,
   boundaries, layers, ownership, and dependencies. For other diagrams, collect the actual
   entities and relationships, ordering, or hierarchy. Do not invent missing behavior or
   relationships.
2. Reduce each node to one concise label while preserving distinctions that change the route,
   state, interaction, or dependency. Keep the user's language and terminology.
3. Choose the simplest layout that keeps the requested relationships traceable. A process
   decision with more than two outcomes should use explicit numbered or named exits, or be split
   into a sub-flow; do not hide outcomes under an unexplained `other` branch. This rule does not
   require branches in architecture diagrams.
4. Render the result in a `text` fenced code block. Put the diagram first, followed only by a
   short legend or an `Assumptions` note when needed.
5. Check the rendered text before answering. For all types, verify language, width, allowed
   geometry, readable labels, and connector clarity. For process flows, verify that actual
   terminals are visible, every non-looping path reaches an end, all actual decision exits are
   labeled, and no node is disconnected. For architecture diagrams, verify that every shown
   component has an intentional relationship or boundary and that no dependency direction is
   misleading. For other diagrams, verify that the layout preserves the source's actual
   hierarchy, order, or relationships.

If the input is ambiguous and the ambiguity changes the topology or dependency direction, ask one
focused question. If it does not, draw the confirmed diagram and list the assumption outside it.
For process loops, label the back path explicitly using the user's language; split the loop into
a detail sub-flow if the return connector would be hard to follow.

## Template

Use these shapes as starting points. Replace every example label with the user's language and
the source's actual terms; do not copy the English equivalents just because they appear in a
generic template.

### Process flow

```text
(開始)
   |
   v
[接收輸入]
   |
   v
{輸入有效？} -- 否 --> [顯示錯誤] --> (結束)
   |
  是
   v
[處理輸入] --> (結束)
```

### Architecture or dependency diagram

Architecture diagrams do not need a start, end, decision, or branch when the source does not
contain one:

```text
[使用者] --> [介面層] --> [應用層] --> [領域層]
```

Honor explicit user preferences such as orientation, symbol set, language, or a request for full
detail, while retaining unambiguous connectors and readable labels.
