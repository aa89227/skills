# Decision Records

Use this reference whenever discussion recording is enabled or being configured. The record is a
time-scoped decision history and durable rationale, not a transcript, current implementation
specification, or promise about future behavior.

## Temporal scope and provenance

Each record describes what was known and decided at a particular point in time. A record does not
replace the current target-branch code, tests, configuration, or other implementation artifacts.
When current behavior matters, inspect those artifacts directly.

For every material statement, distinguish the source and state when relevant:

- user requirement or explicit user decision;
- repository evidence observed at a known revision;
- agent inference or proposal;
- accepted, rejected, or deferred choice; or
- unresolved question.

Do not turn an agent-generated draft into a user-confirmed decision merely because it was shown to
the user. Do not record private reasoning or routine implementation choices that were not material
to the discussion.

## Repository Policy

Append a policy block to the applicable AGENTS.md without rewriting unrelated instructions:

~~~markdown
## Decision records

- Mode: enabled
- Directory: docs/decision-records/
- Filename: DR-{id}.md
- Classification: engineering, business, or mixed
- Update mode: incremental
- Reconfirmation: Do not ask again during ordinary discussions. Ask only when this policy is
  missing, invalid, ambiguous, contradictory, unavailable, or explicitly reconfigured by the user.
~~~

For a disabled policy, retain the same heading and set Mode: disabled. For a custom location,
record the repository-relative directory explicitly. Do not put secrets, tokens, or user-level
paths in the policy. If the repository has no AGENTS.md, create the repository-root file only as
part of the user's confirmed first-use setup.

## ID Allocation

1. Inspect the configured directory before allocating an ID.
2. Treat any filename matching DR-{number}.md as occupied, including existing unpadded files.
3. Choose one greater than the largest occupied number; use at least three digits when formatting
   the new filename (DR-001.md, DR-002.md, ...). Expand the padding when necessary.
4. Never reuse an ID or overwrite a non-record file. Check again immediately before writing; if a
   concurrent record took the ID, allocate the next available number.

## Record Schema

Start each record with YAML frontmatter:

~~~yaml
---
id: DR-001
type: mixed
status: in-progress
title: Short decision title
created: 2026-10-12
updated: 2026-10-12
related: []
---
~~~

Use the actual current date and preserve created on updates. related may contain issue, PR,
document, branch, or other repository-relative references.

Use this body structure, omitting sections that genuinely do not apply:

~~~markdown
# Short decision title

## Context

What problem, opportunity, or question led to the discussion? State scope and non-goals.

## Decision status and evidence

State which material facts are user-confirmed, which are repository evidence, which are agent
proposals or inferences, and which remain unresolved. Link the relevant Issue, code revision, or
other source when known.

## Business decision

Record user, product, policy, stakeholder, or outcome decisions. Use this section for
type: business and type: mixed records.

## Engineering decision

Record architecture, implementation, data, security, operational, or compatibility decisions.
Use this section for type: engineering and type: mixed records.

## Alternatives considered

List viable alternatives and why they were rejected, deferred, or retained.

## Consequences

Record benefits, costs, risks, constraints, and affected work.

## Follow-up

List concrete actions, owners when known, and links to resulting code or tickets.

## Open questions

Keep unresolved questions explicit. Do not present proposals or agent inferences as confirmed
decisions.

## Change log

Record dated material changes to the proposal, decision, or status. Do not copy every discussion
turn.
~~~

## Classification and Status

- engineering: the record is primarily a technical decision.
- business: the record is primarily a product, policy, user, or operational decision.
- mixed: both kinds are material; keep them in separate body sections and explain their
  relationship.
- in-progress: the record is being updated during an active discussion.
- proposed: an approach is recommended but the user has not explicitly confirmed it.
- confirmed: the user explicitly accepted the decision at that time; this does not assert that the
  current implementation or a future requirement still has the same behavior.
- deferred: the question is intentionally postponed.
- superseded: a later record replaces it; link the replacement.
- no-decision: the discussion ended without a material decision.
- abandoned: the user explicitly stopped the discussion without resolving it.

Only use confirmed for an explicit user decision. If the discussion is interrupted, leave the
record in-progress rather than inventing a conclusion. A later discussion may resume that record
only when the user makes that intent clear; otherwise create a new record and link it.

## Consistency Rules

- Record decisions, rationale, alternatives, consequences, and follow-up—not every conversational
  turn.
- Distinguish user-confirmed facts, repository evidence, agent inferences, and unresolved questions.
- Treat current target-branch code, tests, and configuration as the source for implemented behavior;
  do not use a decision record to answer what the system currently does.
- Keep the Issue body and decision record time-scoped. If a later requirement or implementation
  changes the result, preserve the historical record and link or mark the newer source rather than
  silently rewriting the old decision.
- When a material decision changes an Issue requirement, record the rationale and decision state here
  and update the Issue body with the resulting requirement snapshot. The Issue remains the handoff
  requirement for that revision; this record does not become the latest contract, and a separate
  specification artifact must be explicitly identified if the contract is not in the Issue.
- Update the record after each material change and before reporting the discussion's final status.
- Preserve the original context and rationale when revising a proposal; note material changes in
  the change log rather than silently rewriting history.
- Link related Issues, PRs, code, specifications, or prior decision records when known.
- Do not copy credentials, tokens, private data, or unnecessary sensitive business details into the
  record.
- If recording is disabled, do not create a placeholder file. If recording is enabled but no
  decision emerges, keep the record and mark it no-decision so the missing outcome is explicit.
