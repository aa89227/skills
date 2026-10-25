---
name: discuss
description: |
  Enter discussion mode to investigate context, clarify requirements, compare approaches, and
  preserve material decisions in repository decision records while leaving implementation and
  external state unchanged. Remain read-only for all other changes until the discussion is
  complete and the user sends a separate, explicit implementation instruction. Use for
  "discuss", "let's discuss", "discussion mode", "don't change anything yet", "just talk",
  "think together", "討論一下", or "先不要改".
license: MIT
metadata:
  author: aa89227
  version: "1.4"
---

# Discussion Mode

## Instruction Retention

Read this file when this skill is first activated for the current task. Once read, retain and
follow its instructions without rereading the file on every turn or before every action.

Reread it only when the file may have changed, the current context no longer contains its
instructions (for example after context compaction or a new session), the instructions are
ambiguous or conflicting, or exact wording must be verified.

Investigate and discuss the request without implementing it. Discussion mode may persist a
decision record as a narrowly scoped side effect; it must not modify implementation files,
issues, services, or other external state.

## Safety Boundary

1. Do not create, edit, move, or delete implementation files.
2. When an applicable decision-record policy enables recording, create or update only the
   configured record file and its parent directory. A session creates one new record unless the
   user explicitly asks to resume an existing in-progress record.
3. Do not write an `AGENTS.md` or other instruction file until the user explicitly confirms the
   first-use policy setup. That confirmation permits only the configured decision-record policy
   write, never code changes or external mutations.
4. Do not run commands that change services, issue trackers, or other external state.
5. Use read-only inspection and research for all other repository work.
6. If the runtime provides a persistent read-only, planning, or discussion mode, use it as an
   additional safeguard. Do not depend on product-specific modes or tool names for correctness.
7. If a configured record cannot be written, report the failure and do not claim that it was
   saved. Continue the discussion only if doing so does not misrepresent persistence.

## Decision-Record Policy

Resolve the policy before recording any material decision. Apply the first applicable rule:

1. An explicit instruction for the current discussion.
2. The nearest applicable repository `AGENTS.md` rule, or an instruction file explicitly named by
   that rule.
3. The built-in defaults below, after the one-time first-use setup question.

The built-in defaults are:

- recording enabled;
- directory `docs/decision-records/` relative to the repository root;
- filename `DR-{id}.md`, using the next numeric ID with at least three digits (`DR-001.md`);
- one directory with `type: engineering`, `type: business`, or `type: mixed`;
- incremental updates during the discussion;
- both engineering and business decisions may be recorded in the same record.

If no policy exists, ask one compact setup question before the first write. Offer the defaults,
a custom directory or policy, disabled recording, or a session-only choice. Ask whether the
selection should be persisted. If the user confirms persistence, append the policy to the nearest
`AGENTS.md`; if none exists, create a repository-root `AGENTS.md` as part of that confirmed
setup. Do not overwrite unrelated instructions.

The persisted rule must say not to ask again during ordinary discussions. Re-ask only when the
policy is missing, invalid, ambiguous, contradictory, the configured path is unavailable, or the
user explicitly requests reconfiguration. A persisted disabled policy means do not create a
record and do not ask again. A session-only choice ends with the session and may be requested
again in a later session.

When recording is enabled or being configured, read
[references/decision-records.md](references/decision-records.md) before creating or updating a
record.

## Reading Existing Decision Records

Decision records are time-scoped historical context, not the current implementation or an automatic
source of future requirements. A repository policy that enables recording does not require loading
the entire decision-record directory.

- Do not glob or load every `docs/decision-records/*.md` file for an ordinary task.
- If the user names a record or explicitly asks to resume an in-progress record, read that record
  first and preserve its context.
- Otherwise, inspect record filenames, titles, and frontmatter before reading bodies. Select
  candidates using the current request's issue, PR, document, branch, or other exact references in
  `related`.
- Read only selected records and the sections relevant to the current question. Skip `Change log`
  by default; read it when the user asks for history or when the current decision conflicts with
  the record.
- Follow directly linked records only when they are needed to resolve the current question. Do not
  recursively load the whole linked-record graph.
- Treat `status` as the record's discussion lifecycle, not proof that every statement is current
  or universally applicable. `confirmed` means explicitly accepted at the time of the decision; it
  does not mean that the current implementation or every future requirement still agrees. Do not
  infer validity or supersession from `updated` alone.
- Tags are optional retrieval hints, not required metadata and not a source of truth. Do not make
  a task depend on tags being present.
- If no directly related record is found, continue without bulk-loading historical records. Ask a
  focused question only when the missing decision history materially affects the work.

## Context, provenance, and discussion boundary

Keep these sources separate:

- **Current implementation:** the current target-branch source code, tests, configuration, and
  other repository artifacts. Read these when the question is what the system currently does.
- **Issue body:** a time-scoped requirement snapshot for the Issue's stated work. It describes the
  intended scope and acceptance at that Issue revision; it is not proof of current implementation.
- **Decision record:** time-scoped rationale, alternatives, consequences, and decision history. It
  is not a permanent or future specification.

If the user asks about current behavior, inspect the current implementation first. Read an Issue
for its requested scope or lifecycle when that context is needed, and read a decision record when
the user needs the reason for a past choice or the decision history materially affects the work.
Do not trace historical Issues or decision records merely to answer what the code currently does.

The agent MAY independently draft or choose details needed to complete a task. Discussion does not
require the user to decide every field, class, or implementation choice. Record a material item when
it was stated by the user, surfaced by the agent as a meaningful proposal or decision, supported by
repository evidence, or needed to explain a consequential choice. Do not record private reasoning,
every generated option, or routine internal implementation choices.

Use consequence, not technical visibility, to decide whether to ask. The agent may choose external
contract details autonomously when reasonable alternatives have the same intended meaning. Ask a
focused question or pause only when the alternatives change user-visible semantics, compatibility or
migration, security or privacy, cost or operational risk, irreversible data behavior, or conflict
with an explicit requirement or repository evidence. Class names, method boundaries, package choices,
parser algorithms, file layout, test mechanics, and similar routine details do not require a new
discussion unless they create one of those consequences.

For every recorded material item, distinguish its source and state: user requirement, repository
evidence, agent inference, proposed choice, explicitly accepted decision, rejected alternative, or
unresolved question. Seeing a proposal is not the same as explicitly accepting it. An agent may
produce a complete draft without asking the user to decide each detail, but must not describe its
own draft as a user-confirmed historical decision.

When a material discussion decision changes an Issue requirement, keep the two artifacts aligned by
role: record the decision and its rationale in the decision record, and write the resulting
time-scoped requirement snapshot in the Issue body. Link the related artifacts when possible, but do
not copy private reasoning or routine implementation details into the Issue, and do not use the
decision record as the latest contract. A later requirement or implementation change updates the
newer source and preserves the older record as history.

When a specification-type task requires a final representation, the agent must provide the needed
deliverable at the appropriate handoff point. It must not keep discussion open merely to enumerate
every combination or internal detail; cover the semantic branches that change the contract and
defer routine implementation choices.

## Discussion Workflow

1. Identify whether the request concerns current implementation behavior, a time-scoped requirement,
   or historical rationale. Inspect current code for current behavior and only the related Issue or
   decision record needed for the other questions, without making changes.
2. Resolve the decision-record policy and, when enabled, allocate the next record ID and create an
   initial `in-progress` record containing the request and current context.
3. Summarize the current understanding, including constraints and consequential unknowns.
4. Ask focused clarification questions only when their answers affect the decision.
5. After each material decision, rejected alternative, or consequential unresolved question,
   update the record in place. Keep the record concise and current rather than copying the chat
   transcript.
6. Compare viable approaches and explain their trade-offs, risks, and assumptions.
7. Recommend an approach when the available evidence supports one.
8. Continue until no material decisions remain unresolved, or clearly mark the record as proposed,
   deferred, no-decision, or abandoned.

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
3. If recording is enabled, persist the updated decision record; make no other repository or
   external changes.
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
