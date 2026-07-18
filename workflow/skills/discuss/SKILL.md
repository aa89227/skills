---
name: discuss
description: |
  Enter discussion mode to investigate context, clarify requirements, and compare approaches
  without making changes. Remain read-only until the discussion is complete and the user sends
  a separate, explicit implementation instruction. Use for "discuss", "let's discuss",
  "discussion mode", "don't change anything yet", "just talk", "think together", "討論一下",
  or "先不要改".
license: MIT
metadata:
  author: aa89227
  version: "1.1"
---

# Discussion Mode

Investigate and discuss the request without implementing it.

## Safety Boundary

1. Do not create, edit, move, or delete files.
2. Do not run commands that change repository, system, service, or external state.
3. Use only read-only inspection and research while gathering context.
4. If the runtime provides a persistent read-only, planning, or discussion mode, use it as an
   additional safeguard. Do not depend on product-specific modes or tool names for correctness.

## Discussion Workflow

1. Inspect relevant context without making changes.
2. Summarize the current understanding, including constraints and consequential unknowns.
3. Ask focused clarification questions only when their answers affect the decision.
4. Compare viable approaches and explain their trade-offs, risks, and assumptions.
5. Recommend an approach when the available evidence supports one.
6. Continue until no material decisions remain unresolved.

Use the runtime's structured user-input mechanism when helpful. Use it only to gather requirements
or preferences, never to request implementation approval.

## Authorization Boundary

Treat every answer to an agent-initiated question as requirement input, never as authorization to
implement. Selecting a recommended option, agreeing with an approach, or answering with phrases
such as "go ahead" does not authorize implementation when it is part of a clarification response.

After every answer to a clarification question:

1. Update the current understanding.
2. Explain the resulting decision or remaining trade-off.
3. Do not make changes or perform actions with side effects.
4. Stop after presenting the updated discussion state.

When no material questions remain, present the final proposed direction and end the response with
this exact standalone line:

```text
STATUS: READY_FOR_IMPLEMENTATION
```

Do not ask whether implementation should begin in that response. Implementation is authorized only
when the user sends a later, separate message that explicitly requests implementation, execution,
or file changes. A message that both answers a clarification question and requests implementation
does not cross this boundary; first present the updated direction and readiness status.

After valid authorization, state that discussion mode has ended and proceed with implementation.
