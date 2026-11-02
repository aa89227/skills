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

The Issue body is the canonical time-scoped requirement snapshot for the Issue's current revision.
It describes the intended scope, acceptance, and constraints at that point in the Issue lifecycle;
it is not proof of current implementation behavior. When a requirement changes, update the body
before changing lifecycle or implementation. Do not leave the new requirement only in a comment.
Comments MAY record questions, decisions, and history, but a new agent MUST be able to recover the
Issue's requested work from the body without reading a long comment thread.

The body is the resulting requirement snapshot, not a transcript of everything the agent said. Keep
material unresolved questions explicit, but do not copy private reasoning or routine implementation
choices into the requirement merely because they appeared in discussion.

The current target-branch source code, tests, configuration, and other repository artifacts are the
authority for what is actually implemented. A decision record preserves time-scoped rationale and
does not replace either the current implementation or the Issue's current requirement snapshot.
When those sources disagree, report and reconcile the difference; do not silently treat a historical
record as a future specification or infer current behavior from the Issue body.

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

## Requirement shapes and primary representation

The following are drafting shapes, not new Issue Types, lifecycle statuses, or `Work Mode` values.
Keep the repository's existing Issue Type and Work Mode metadata. Select the shape that best
describes the Issue's main deliverable:

| Shape | Primary representation | Minimum content |
| --- | --- | --- |
| Ordinary requirement | Observable outcome and scenario(s) | Problem, expected outcome, scope, acceptance |
| Specification | Contract, field responsibilities, and semantic-variant examples | Actual contract, applicable behavior rules, acceptance |
| Research | Question, evidence, conclusion, and unresolved items | Bounded question, evidence plan, decision criteria |
| Bug | Reproduction and actual-versus-expected behavior | Reproduction, expected result, regression verification |
| Refactor | Behavior-preservation boundary and verification | Preserved behavior, technical boundary, validation |
| Initiative | Product outcome and native child map | Parent outcome, scope, child Requirements, aggregate success |

Use one primary definition location for each material requirement:

- external shape and field meaning belong in `Contract / Format Specification`;
- allowed or forbidden states belong in domain rules or acceptance criteria;
- quality thresholds belong in `Constraints` or the relevant acceptance criterion;
- observable proof belongs in `Acceptance Criteria`;
- internal implementation choices belong in `Implementation Notes`.

Other sections MAY reference or verify the primary definition, but MUST NOT copy it in full. Omit
conditional sections that would contain only `None` or a duplicate.

Use Given/When/Then only when an event produces an observable result. Do not use it to describe a
field list, class design, research question, or pure behavior-preserving refactor. Add another
scenario only when the actor, result, permission, data responsibility, persistence behavior, or
error behavior materially differs. Use an example or variant table for multiple values of the same
behavior. Split an Issue only when the parts can be independently delivered, reviewed, and accepted;
success, failure, permission, and alternate paths usually remain in one logical requirement.

### English drafting template

The following template is a stable structural reference for agents. Actual Issue prose and headings
follow the selected Issue language.

```text
## Problem / Purpose

## Expected Outcome

## Scenario(s)                 <!-- conditional: observable behavior -->

## Contract / Format Specification  <!-- conditional: specification deliverable -->

## Scope
### In Scope
### Out of Scope                  <!-- conditional when meaningful -->

## Acceptance Criteria

## Constraints                    <!-- conditional when meaningful -->
## Edge Cases                     <!-- conditional when behavior changes -->
## Terms / Glossary               <!-- conditional when jargon remains -->
## Open Questions                 <!-- conditional when unresolved -->

## Release Note
## Workflow Metadata
## Implementation Notes           <!-- conditional; always last when present -->
```

English examples:

```text
## Problem / Purpose

People can save a record, but reopening it does not preserve the meaning of the selected options.

## Expected Outcome

The saved record can be reopened and saved again without losing the selected options.

## Scenario

Given a user has selected valid options,
When the user saves and reopens the record,
Then the same options are displayed and can be saved again without semantic loss.
```

For a specification shape, the contract itself remains in the Issue:

```text
## Contract / Format Specification

The payload contains a stable version, a required identifier, and a list of entries. The example
below covers the ordinary case; additional examples cover semantic variants that change validation,
permissions, persistence, or compatibility behavior.
```

Do not turn routine implementation choices into questions just because the Issue uses a technical
contract. Ask the user only when alternatives change user-visible meaning, compatibility or
migration, security or privacy, cost or operational risk, irreversible data behavior, or conflict
with explicit requirements or repository evidence.

## Reader-first writing rules

The Issue is the requirement source of truth for its current Issue revision and MUST be
understandable by a non-engineering reader. It is not the source for current implemented behavior.
The agent MUST write the title, problem or purpose, expected outcome, scope, applicable acceptance
criteria, and release note in plain language focused on user impact and observable outcomes. Add
constraints, edge cases, glossary entries, and open questions only when they carry information that
changes the requirement or its verification.

- MUST NOT add engineering terminology merely because it is familiar to the agent.
- MAY use a technical term when it is necessary to define a constraint, behavior, risk, or
  acceptance condition.
- When a technical term is necessary, the first occurrence MUST include a plain-language
  explanation or a definition in `Terms / Glossary`.
- Exact technical identifiers, architecture details, API names, commands, and implementation
  mechanics MUST be kept in the final `Implementation Notes` section unless an earlier section
  needs the exact term to make the requirement unambiguous. When the Issue's deliverable is a
  schema, data format, protocol, API contract, content model, serialization format, or editor block
  format, the contract itself is requirement content and MUST NOT be moved to `Implementation Notes`
  merely because it is technical.
- `Implementation Notes` MUST be the last Issue section when present. It is for technical context
  needed by implementers, not a second copy of the requirement.

Acceptance criteria SHOULD describe what a user, operator, or reviewer can observe. A technical
test or internal check MAY be included when it is necessary evidence, but it MUST be accompanied by
the plain-language outcome it protects.

## Specification-type requirements

A specification-type Issue is one whose deliverable is a schema, data format, protocol, API
request/response contract, content model, serialization format, grammar, Markdown or editor block
format, or another contract intended to be consumed or persisted by an implementation.

The agent MAY draft and complete the contract without asking the user to decide every field. The
Issue MUST distinguish an agent-generated draft or inference from an explicitly accepted decision;
the user's silence or exposure to a proposal is not acceptance. Before `Specifying` → `Ready`, the
Issue body or an explicitly versioned durable artifact MUST contain the actual contract needed for
handoff. A linked artifact used for this purpose MUST be an explicitly versioned specification
artifact, not a decision record. The contract must include:

- the outer representation or grammar;
- the responsibility and requiredness of persisted or consumed fields;
- version, identifier, default, nullability, unknown-data, and unsupported-version behavior when
  applicable;
- representative examples covering semantic variants, not every possible combination; and
- persistence, round-trip, compatibility, error, or other observable behavior that applies.

If an Acceptance Criterion says that a representative example is provided, the Issue body MUST
contain at least one complete example before that criterion is treated as satisfied. A promise to
provide one later, an empty placeholder, or an example hidden only in `Implementation Notes` does
not satisfy the criterion. If the contract is not complete, keep the missing item explicit as a
blocking `Open Questions` entry and keep the Issue in `Specifying`.

This gate does not require the user to pre-approve each field or require Discuss to enumerate every
internal design choice. Ask for human input only when competing choices change user-visible meaning,
compatibility or migration, security or privacy, cost or operational risk, irreversible data
behavior, or conflict with an explicit requirement or repository evidence. It requires the final
requirement snapshot to be usable without guessing.
Parser names, class structure, package choice, commands, database mechanics, and test-framework
choices remain implementation details unless they change the external requirement.

## Mandatory Reader-first preflight

Complete this preflight before calling GitHub to create or update an Issue. It is a publication
gate, not an optional review note. If any item fails, keep the draft local and rewrite it before
publishing. Ask the user only when the intended reader, language, or observable outcome cannot be
reasonably determined.

- **Audience and language:** The title, reader-facing sections, and `Release Note` use the user's
  preferred language and are understandable to a non-engineering reader.
- **Plain-language outcome:** `Problem / Purpose` and `Expected Outcome` explain the problem and
  desired result without requiring knowledge of the implementation.
- **Technical-term scan:** Identify every acronym, framework name, API/class name, architecture
  term, test tool, and implementation verb that a non-engineering reader may not know. For each term
  that remains in a reader-facing section, its first occurrence has a short plain-language
  explanation, for example `通知方式（notification method）`. A glossary entry by itself does not
  excuse an unexplained first occurrence.
- **Technical-detail boundary:** Move exact API names, commands, schemas, test frameworks, package
  names, and implementation mechanics to `Implementation Notes` unless the exact term is required
  to make the behavior unambiguous. For a specification-type Issue, do not move the required
  contract, field semantics, representative examples, or observable serialization behavior there;
  explain the technical term and keep the contract in the requirement sections or linked durable
  artifact.
- **Observable acceptance:** Every acceptance criterion uses `- [ ]` and describes a result that a
  user, operator, or reviewer can observe. Pair any internal test or implementation condition with
  the user-facing outcome it protects.
- **Glossary completeness:** Use `Terms / Glossary` for every necessary technical term that cannot
  be explained inline. Use `None` only when no such terms remain.
- **Mode and representation:** Select one drafting shape, use its primary representation, and do
  not require a Scenario, Contract, research evidence, or behavior-preservation section when that
  shape does not need it.
- **Structure:** Core sections appear in the canonical order, conditional sections appear only when
  meaningful, `Open Questions` uses the required table shape when present, unresolved placeholders
  are removed, and `Implementation Notes` is the final section when present.
- **No duplication:** Each material requirement has one primary definition location. Summary-like,
  rationale-like, or edge-case text is not repeated in multiple sections.
- **Scenario fit:** Given/When/Then is used only for an event with an observable result. Field lists,
  contracts, research questions, and routine implementation details use their appropriate form.
- **Specification contract:** If the Issue is specification-type, verify that the actual contract,
  semantic-variant examples, and applicable round-trip, compatibility, error, or unknown-data rules
  are present in the Issue or in an exact versioned, directly linked specification artifact. A
  decision record does not satisfy this requirement. Do not require exhaustive examples for
  combinations that do not change the contract.
- **Requirement provenance:** Distinguish user requirements, repository evidence, agent inferences,
  proposed choices, accepted decisions, and unresolved questions. Do not present an AI-generated
  draft as a user-confirmed decision.
- **Current behavior check:** When the Issue makes a claim about what the system currently does,
  verify it against the current target-branch code, tests, and configuration; do not infer it from an
  Issue body or decision record.
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

Use the following semantic order. The core sections are `Problem / Purpose`, `Expected Outcome`,
`Scope`, `Acceptance Criteria`, `Release Note`, and `Workflow Metadata`. The other sections are
conditional: include them only when they carry information that changes the requirement, its
verification, or its handoff. Heading labels MAY be localized; the prose is the current
time-scoped requirement snapshot, not a comment-thread transcript or a claim about current
implementation behavior.

```markdown
## Problem / Purpose

<!-- What problem exists, who or what is affected, and why it matters. -->

## Expected Outcome

<!-- What observable result or deliverable this Issue must produce. -->

## Scenario(s)

<!-- Conditional: use only for event-driven observable behavior. -->

## Scope

### In Scope

<!-- What this Issue is responsible for. -->

### Out of Scope

<!-- Conditional: include only meaningful exclusions or delegated boundaries. -->

## Contract / Format Specification

<!-- Required only for a specification-type Issue. Include the actual contract or an exact,
     versioned specification-artifact reference and at least one complete representative example. -->

## Acceptance Criteria

- [ ] An observable criterion.

## Constraints

<!-- Conditional: include only meaningful quality, security, compatibility, or operational limits. -->

## Edge Cases

<!-- Conditional: include only alternate behavior that changes verification or user impact. -->

## Terms / Glossary

<!-- Conditional: include only necessary terms not explained at first use. -->

## Open Questions

<!-- Conditional: include only unresolved questions that affect delivery. -->

| ID | Question | Blocking | Resolution |
| --- | --- | --- | --- |

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

<!-- Conditional: include only non-requirement technical handoff details; always keep this last. -->
```

The template is a shape, not permission to retain unresolved placeholders or empty sections. Before
`Specifying` → `Ready`, remove unused conditional sections, replace every remaining placeholder,
mark acceptance criteria accurately, and resolve or explicitly classify every open question. A row
with `Blocking: Yes` and no resolution prevents `Ready`. If a technical term is used, explain it at
first use or complete `Terms / Glossary`; keep internal implementation details in the final
`Implementation Notes` section while keeping any required external contract in the requirement
sections or linked durable artifact.

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
