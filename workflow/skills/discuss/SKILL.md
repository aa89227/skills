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
  version: "1.2"
---

# Discussion Mode

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

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

## Completion and Next Step

When handing control back to the user, emit these two fields in this order:

```text
STATUS: <stable status token>
NEXT_STEP: <user-facing instruction>
```

`STATUS` is machine-readable and MUST use one of these values:

- `DISCUSSION_COMPLETE`: the discussion reached a stable conclusion. This does not imply that
  implementation is needed or authorized.
- `IMPLEMENTATION_PLAN_READY`: the discussion produced a sufficiently specific implementation
  direction. This does not authorize changes; a separate explicit implementation request is still
  required.
- `NEEDS_USER_INPUT`: a material decision remains unresolved and the agent is asking a focused
  question before the discussion can conclude.

`NEXT_STEP` is for the user, not the workflow parser. It MUST:

- explicitly state what the user needs to do, if anything;
- say clearly when no action is required; and
- be written in the user's language, following an explicit language preference when provided.

Do not use `READY_FOR_IMPLEMENTATION`; it conflates a completed discussion with implementation
readiness and authorization. Do not treat `NEXT_STEP` as implementation authorization either.

## Authorization Boundary

Treat every answer to an agent-initiated question as requirement input, never as authorization to
implement. Selecting a recommended option, agreeing with an approach, or answering with phrases
such as "go ahead" does not authorize implementation when it is part of a clarification response.

After every answer to a clarification question:

1. Update the current understanding.
2. Explain the resulting decision or remaining trade-off.
3. Do not make changes or perform actions with side effects.
4. Stop after presenting the updated discussion state.

When the discussion reaches a stable conclusion, present the final proposed direction and end the
response with `STATUS` followed by `NEXT_STEP`. Use `DISCUSSION_COMPLETE` for a discussion that
does not produce an implementation plan, and use `IMPLEMENTATION_PLAN_READY` only when a concrete
implementation direction has been agreed. If a material question remains, use `NEEDS_USER_INPUT`
and ask the focused question in the user's language.

```text
STATUS: DISCUSSION_COMPLETE
NEXT_STEP: <user-facing instruction in the user's language>
```

Do not ask whether implementation should begin in that response. Implementation is authorized only
when the user sends a later, separate message that explicitly requests implementation, execution,
or file changes. A message that both answers a clarification question and requests implementation
does not cross this boundary; first present the updated direction and readiness status.

After valid authorization, state that discussion mode has ended and proceed with implementation.
