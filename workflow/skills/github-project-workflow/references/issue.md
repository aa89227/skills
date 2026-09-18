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

## Mandatory Reader-first preflight

Complete this preflight before calling GitHub to create or update an Issue. It is a publication
gate, not an optional review note. If any item fails, keep the draft local and rewrite it before
publishing. Ask the user only when the intended reader, language, or observable outcome cannot be
reasonably determined.

- **Audience and language:** The title, reader-facing sections, and `Release Note` use the user's
  preferred language and are understandable to a non-engineering reader.
- **Plain-language outcome:** `Summary`, `Background / Problem`, and `Goal` explain the problem and
  desired result without requiring knowledge of the implementation.
- **Technical-term scan:** Identify every acronym, framework name, API/class name, architecture
  term, test tool, and implementation verb that a non-engineering reader may not know. For each term
  that remains in a reader-facing section, its first occurrence has a short plain-language
  explanation, for example `通知方式（notification method）`. A glossary entry by itself does not
  excuse an unexplained first occurrence.
- **Technical-detail boundary:** Move exact API names, commands, schemas, test frameworks, package
  names, data structures, and implementation mechanics to `Implementation Notes` unless the exact
  term is required to make the behavior unambiguous. When it remains earlier, explain why the exact
  term matters.
- **Observable acceptance:** Every acceptance criterion uses `- [ ]` and describes a result that a
  user, operator, or reviewer can observe. Pair any internal test or implementation condition with
  the user-facing outcome it protects.
- **Glossary completeness:** Use `Terms / Glossary` for every necessary technical term that cannot
  be explained inline. Use `None` only when no such terms remain.
- **Structure:** Required sections appear in the canonical order, `Open Questions` uses the required
  table shape, unresolved placeholders are removed, and `Implementation Notes` is the final section.
- **Final reader pass:** Read only the title through `Release Note` as if you were the intended
  non-engineering reader. If the reader would need to ask what a term means or why an implementation
  choice matters, rewrite that passage before publishing.

Do not treat English text itself as a failure. Product names and exact technical identifiers may
remain when necessary; the failure is unexplained jargon or implementation detail in a section that
should communicate user impact.

Examples in this reference MUST remain domain-neutral. Do not reuse the triggering Issue's product,
API, framework, class, or architecture names in an example; examples demonstrate the rewrite pattern,
not the Issue's technical domain.

### Reader-first drafting example

Avoid putting the implementation in the goal:

```text
Use a new internal mechanism to process the submitted data and update the view.
```

Prefer a user-facing outcome:

```text
使用者送出資料後，畫面會顯示更新後的結果。
```

Keep the exact implementation vocabulary for implementers:

```text
Implementation Notes:
- Validate the submitted input before processing.
- Save the processed result in the appropriate application state.
- Return a stable result summary for verification.
```

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
Every Issue that produces a repository artifact MUST include `Review Mode: Collaborative` or
`Review Mode: Solo Maintainer`. Use `Collaborative` by default. A human may explicitly select
`Solo Maintainer` only when the PR author is the sole maintainer/reviewer for the current work and
no independent reviewer is available; the agent MUST NOT infer that mode. A no-artifact discovery
Issue does not need this field.

## Design System Workflow Metadata

When an Issue or request involves Design System, design tokens, visual rules, theme, component
contract, shared CSS, accessibility contract, conformance, or a new Design System component, add
these fields to `Workflow Metadata` and apply
[design-system-handoff.md](design-system-handoff.md). The agent infers the values from the request
and available evidence; the requester does not need to choose tokens, APIs, ARIA, DOM, or test
tools.

```text
Design System Scope: None | Design | Build | Design+Build
Design System Phase: Not Required | Experience Intake | Contract Draft | Contract Approved
Design System Approval: Pending | Approved
Implementation Gate: Blocked | Open
```

Their meanings are:

- `Design System Scope: None`: no shared Design System decision or artifact is involved; use the
  ordinary workflow. `Design`, `Build`, and `Design+Build` mean design-only contract work,
  implementation against an approved contract, or both phases in that order.
- `Design System Phase: Not Required`: only for `Scope: None`. `Experience Intake` means the
  experience is still being clarified, `Contract Draft` means the brief is confirmed but the
  contract packet is not fully approved, and `Contract Approved` means the required packet is
  approved and mapped.
- `Design System Approval: Pending`: a required Design System artifact is not yet approved. For
  `Scope: None` it is not applicable and does not block ordinary work. `Approved` requires the
  confirmed Experience Brief, approved Component Specification, recorded Accessibility
  requirements, and approved Test/Conformance Specification with completed mapping.
- `Implementation Gate: Blocked`: a non-`None` Design System Issue cannot enter `In Progress`,
  create an implementation branch, modify runtime code, or create an implementation PR. `Open`
  means the current scope's prerequisites and implementation plan/validation targets are ready.

`Issue Ready` remains Requirement Approval only and does not set `Design System Approval` to
`Approved`. These metadata fields do not create or rename Project Status values. For the complete
routing rule, design questions, mixed-Issue handling, and examples, read
[design-system-handoff.md](design-system-handoff.md).

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
- Design System Scope: None
- Design System Phase: Not Required
- Design System Approval: Pending
- Implementation Gate: Open
- Approval Source: None
- Review Mode: Collaborative
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

For a Design System request, a changed Experience Brief or any user-visible contract decision also
invalidates the Design System Approval and Implementation Gate. Set the phase back to `Experience
Intake` or `Contract Draft`, set approval to `Pending`, and set the gate to `Blocked`; if the change
also changes the Issue requirement, return the Project Status to `Specifying` and obtain new
Requirement Approval.

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
