# Issue Specification

Use this reference when creating or revising a GitHub Issue, deciding whether a requirement is
ready for implementation, or recording a requirement/release-note change. For parent Initiative
decomposition and discovery work, also read [epic.md](epic.md).

## Requirement boundary

One Issue MUST represent one logical requirement. Implementation size, number of files, number of
PRs, or use of a stack MUST NOT by itself cause the agent to split the Issue. Split only when the
work contains separate logical requirements and the human or repository policy authorizes that
split. Use multiple or stacked PRs for a large implementation of one requirement.

An `Issue Role: Initiative` parent is a planning-container exception: it groups two or more child
Requirements through GitHub's native parent/sub-issue relationship and has no implementation PR of
its own. Each child remains one logical requirement and follows this specification.

The Issue body is the canonical current specification. When a requirement changes, update the body
before changing lifecycle or implementation. Do not leave the new requirement only in a comment.
Comments MAY record questions, decisions, and history, but a new agent MUST be able to work from the
body without reading a long comment thread.

## Issue language

When creating or updating an Issue, the agent MUST write the title, body, and Release Note in the
user's explicitly preferred language. If no preference is stated, use the primary language of the
current user request. Do not hard-code English, Chinese, or another output language in the workflow.

The semantic headings shown below MAY be translated into the selected language, but the agent MUST
keep lifecycle values, GitHub field values, code identifiers, commands, branch names, and exact
machine-readable tokens unchanged. Use one selected language consistently within an Issue.

The agent MUST persist the selected language in the Issue's `Workflow Metadata` as `Language` so a
later session does not depend on chat context. A later Human language preference overrides the
stored value and MUST update it.

## Reader-first writing rules

The Issue is the requirement source of truth and MUST be understandable by a non-engineering
reader. The agent MUST write the title, problem, goal, scope, acceptance criteria, constraints,
edge cases, and release note in plain language focused on user impact and observable outcomes.

- MUST NOT add engineering terminology merely because it is familiar to the agent.
- MAY use a technical term when it is necessary to define a constraint, behavior, risk, or
  acceptance condition.
- When a technical term is necessary, the first occurrence MUST include a plain-language
  explanation or a definition in `Terms / Glossary`.
- Exact technical identifiers, architecture details, API names, commands, schemas, and
  implementation mechanics MUST be kept in the final `Implementation Notes` section unless an
  earlier section needs the exact term to make the requirement unambiguous.
- `Implementation Notes` MUST be the last Issue section. It is for technical context needed by
  implementers, not a second copy of the requirement.

Acceptance criteria SHOULD describe what a user, operator, or reviewer can observe. A technical
test or internal check MAY be included when it is necessary evidence, but it MUST be accompanied by
the plain-language outcome it protects.

## Required issue fields

Every Issue MUST have a type from this closed set:

`Feature`, `Bug`, `Improvement`, `Refactor`, `Chore`, `Documentation`.

Set the repository's native Issue Type field when available. If it is unavailable, record the type
in the Issue body under `Workflow Metadata`; do not invent another type.

The GitHub Milestone field represents the planned release. It MAY be unset (`None`), but an agent
MUST NOT guess a release version. Milestone and lifecycle Status are independent dimensions; for
example, `In Progress` plus milestone `v1.4.0` is valid.

When a Project exposes a `Priority` field, every Issue MUST use one of `Untriaged`, `P0`, `P1`, or
`P2`, with `Untriaged` as the default when triage evidence is insufficient. If the field is not
available, record the same value in `Workflow Metadata`. The Issue body MUST contain a
`Priority Rationale` whenever the value is assigned or changed; the rationale must state both the
reason and its evidence. `P1` requires an existing or Human-supplied exact Milestone or Target
Date. Do not invent a date, Milestone, or Priority commitment.

Use the Project's existing `Blocked`, `Blocked By`, and `Unblocking Condition` fields when
available. Otherwise use the `Workflow Metadata` block below. Keep the metadata truthful; a block
does not change lifecycle Status.

## Canonical Issue body

Use these semantic sections in this order. The heading labels are illustrative semantic names and
MAY be localized; the prose under each section is the current value, not a placeholder for a
comment thread.

```markdown
## Summary

<!-- One concise statement of the requirement. -->

## Background / Problem

<!-- What problem exists, who/what is affected, and why it matters. -->

## Goal

<!-- Observable outcome this Issue must deliver. -->

## Scope

<!-- Included behavior and implementation boundaries. -->

## Out of Scope

<!-- Explicit exclusions. Write None only when there are no meaningful exclusions. -->

## Acceptance Criteria

- [ ] An observable criterion.

## Constraints

- None

## Edge Cases

- None

## Terms / Glossary

- None

## Open Questions

| ID | Question | Blocking | Resolution |
| --- | --- | --- | --- |
| None | None | No | None |

## Release Note

None

## Workflow Metadata

- Language: <selected user language>
- Issue Role: Requirement
- Parent Issue: None
- Work Mode: Delivery
- Approval Source: None
- Type: Feature
- Priority: Untriaged
- Priority Rationale: None
- Blocked: No
- Blocked By: None
- Unblocking Condition: None

## Implementation Notes

- None
```

The template is a shape, not permission to retain unresolved placeholders. Before `Specifying` →
`Ready`, replace every placeholder, mark acceptance criteria accurately, and resolve or explicitly
classify every open question. A row with `Blocking: Yes` and no resolution prevents `Ready`. If a
technical term is used, complete `Terms / Glossary`; keep its exact implementation details in the
final `Implementation Notes` section.

## Release Note rules

`Release Note` MUST describe the user-visible change in release-note language. It MUST NOT be
generated from commit messages. If the result has no user-visible change, the exact value MUST be
`None`; the agent MUST NOT manufacture a benefit for an internal change.

- `Feature`, `Bug`, and `Improvement` SHOULD have a non-`None` release note when users can observe
  the result. They MAY be `None` when the change is genuinely not user-visible.
- `Documentation` MAY have a release note when the documentation is user-facing.
- `Refactor` and `Chore` MAY use `None`; they SHOULD use `None` when the change is internal.

The release note describes the completed user-facing result in the selected Issue language, not a
promise, implementation plan, or unfinished work. Update it when the delivered behavior changes.

## Specification changes

The following are material requirement changes and MUST return the lifecycle to `Specifying` from
`Ready`, `In Progress`, `In Review`, or `Ready to Merge`:

- expected behavior;
- acceptance criteria;
- scope or out-of-scope;
- user-visible behavior; or
- an important constraint.

Changing Priority alone is not a material requirement change and does not require a lifecycle
transition. If the new rationale reveals a change to behavior, scope, acceptance criteria, or an
important constraint, apply the material-change rule above.

Changing an implementation detail without changing those requirements does not require a new
specification approval. When in doubt, treat the change as material and stop for explicit human
direction rather than silently preserving a stale approval.

## Issue closure

Issue open/closed state is administrative and MUST NOT be used as a substitute for Project Status.
Keep an active lifecycle Issue open. Close it after recording `Done` or `Cancelled`. A closed Issue
with an active Project Status is a reconciliation inconsistency; investigate its cause before
changing either record.
