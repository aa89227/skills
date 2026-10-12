# Decision Records

Use this reference whenever discussion recording is enabled or being configured. The record is a
durable handoff, not a transcript of the conversation.

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
- confirmed: the user explicitly accepted the decision.
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
- Update the record after each material change and before reporting the discussion's final status.
- Preserve the original context and rationale when revising a proposal; note material changes in
  the change log rather than silently rewriting history.
- Link related Issues, PRs, code, specifications, or prior decision records when known.
- Do not copy credentials, tokens, private data, or unnecessary sensitive business details into the
  record.
- If recording is disabled, do not create a placeholder file. If recording is enabled but no
  decision emerges, keep the record and mark it no-decision so the missing outcome is explicit.
