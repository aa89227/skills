# GitHub Project Workflow Initialization

Use this reference when the user asks to initialize, configure, or inspect the GitHub Project
workflow. The goal is to establish a consistent Project, Issue lifecycle, workflow automation, and
Label convention without changing external state before the user has reviewed the proposal.

## Operating boundary

- The initial inspection MUST be read-only. Inspect the current repository, GitHub Project, Issues,
  pull requests, Project Status, Project workflow/automation rules, Labels, and available GitHub
  permissions before proposing changes.
- A request to initialize or configure is not, by itself, confirmation to mutate GitHub. First
  report the current state, recommended state, and proposed changes.
- Before explicit confirmation, MUST NOT create, modify, rename, or delete any Project field,
  workflow, automation, Issue, PR, or Label. Do not add items to a Project or change Project Status.
- After confirmation, apply only the listed minimum changes. If the scope changes, produce a new
  report and obtain confirmation for the new changes.
- If a required permission or Project capability is unavailable, report the exact blocker and
  continue only with read-only inspection that remains authorized.

## Permission checks

Check the minimum GitHub token scope needed for each requested operation:

| Operation | Required scope |
| --- | --- |
| Read Project configuration and items | `read:project` |
| Create or modify Project fields, workflows, or automations | `project` |
| Create or modify repository Issues or Labels | `repo` |

Report missing scopes explicitly. Do not treat a missing write scope as permission to use a
different mutation path.

## Read-only inventory

Inspect and report these facts before recommending setup changes:

1. Repository identity, default/target branch, and relevant repository policy.
2. Project identity and access; all fields, field values, item types, and current Project Status
   values.
3. Project workflow rules, including Auto-add to project, Item added to project, and existing
   automations that change Status or close Issues.
4. Relevant existing Issues and PRs, including their Project membership, Status, Type, Priority,
   lifecycle state, and relationship to the repository.
5. Repository Labels, including exact spelling, case, descriptions, and likely duplicate or
   synonymous names.
6. Available permissions and the scopes needed for each proposed mutation.

The report MUST distinguish observed facts from recommendations. Do not infer a missing field or
workflow rule merely because it is not visible in one Issue or PR.

## Standard lifecycle Status

The Project Status field MUST expose exactly these standard lifecycle values:

`Inbox`, `Specifying`, `Ready`, `In Progress`, `In Review`, `Ready to Merge`, `Done`, `Cancelled`.

Their meanings are:

| Status | Meaning |
| --- | --- |
| `Inbox` | The requirement exists but has not been formally organized. |
| `Specifying` | The canonical requirement is being clarified and completed. |
| `Ready` | A human has approved the current Issue requirement, scope, and acceptance criteria. For Design System work, this is Requirement Approval only; it does not approve the Design System contract. |
| `In Progress` | Approved implementation or bounded discovery work has started. |
| `In Review` | The complete implementation or discovery result is ready for review. |
| `Ready to Merge` | Human approval has been obtained and the result may be integrated. |
| `Done` | The approved result is integrated, verified, and satisfies the completion criteria. |
| `Cancelled` | A human explicitly decided that the requirement will not proceed. |

Design System phases, approvals, and implementation gates are Workflow Metadata, not additional
Project Status values. Use [design-system-handoff.md](design-system-handoff.md) for their values and
gate checks; do not add `Designing`, `Contract Review`, `Design Approved`, or another lifecycle
Status.

`Done` MUST NOT be used until implementation is integrated, required tests/checks pass, the remote
target branch is verified, and acceptance criteria are satisfied. `Cancelled` requires a separate
explicit human decision. See [lifecycle.md](lifecycle.md) for the complete transition matrix.

If these variants exist, report a rename or removal recommendation but do not apply it without
confirmation:

| Observed value | Recommendation |
| --- | --- |
| `Backlog` | Rename to `Inbox` when it represents unorganized incoming work. |
| `In progress` | Rename to `In Progress`. |
| `In review` | Rename to `In Review`. |

For any other Status value, investigate its meaning and propose an explicit rename or removal.
Never silently map an unknown Status to a standard value, and never use a Label as a substitute for
Status.

## Recommended Project workflow

### Auto-add to project

Unless the user requests a different scope, recommend:

- Repository: the current repository.
- Filter: `is:issue`.

If the user wants PRs visible in the Project, recommend `is:issue,pr` and explain that PRs are
implementation review items, not new requirement Issues.

### Item added to project

Recommend a rule that applies only to Issues:

- When: issues only.
- Set: `Status: Inbox`.

Do not apply `Status: Inbox` to PRs. A PR follows the lifecycle of its Primary Issue.

### Automations to review

Inspect these automations and recommend disabling them unless repository policy explicitly requires
them:

- Pull request merged → `Done`.
- Issue closed → `Done`.
- Status changed to `Done` → close Issue.

These automations can bypass the transition protocol, implementation verification, remote target
branch checks, or explicit human approval. Disabling an automation does not prevent the agent from
closing an Issue after verified `Done` under the normal lifecycle rules.

## Label convention

Read the repository's existing Labels before proposing any addition. Reuse an exact existing Label;
do not create duplicate, case-only, or synonymous names. Labels are for classification and search,
never for lifecycle control.

When no equivalent repository classification exists, the following Labels MAY be proposed:

### Type Labels

- `type: feature`
- `type: improvement`
- `type: bug`
- `type: refactor`
- `type: chore`
- `type: docs`

Use only the types relevant to the repository. A native GitHub Issue Type remains authoritative when
available; if it is unavailable, the Issue MUST still record its required Type in `Workflow Metadata`
as defined by [issue.md](issue.md). Type Labels do not control lifecycle.

### Area Labels

Choose only areas supported by the actual repository structure:

- `area: core`
- `area: web`
- `area: infrastructure`
- `area: ai`
- `area: research`
- `area: e2e`

Do not create every area Label by default, and do not infer an area solely from the requested
feature's wording.

Never create these Label categories:

- `status: ready`
- `status: in-progress`
- `status: review`
- `status: done`
- `priority: p0`
- `priority: p1`
- `priority: p2`

Use the Project Status field for lifecycle and the Project `Priority` field, or Issue `Workflow
Metadata` fallback, for Priority. Follow [priority.md](priority.md) for its values and rationale.

## Initialization report

After read-only inspection, report in this order:

1. Permission check results and missing scopes.
2. Current Project fields and Status values.
3. Current Project workflow and automations.
4. Current repository Labels.
5. Recommended additions, modifications, renames, or removals.
6. Items that do not need changes.
7. The minimum mutation list required to reach the recommended state.
8. A request for explicit confirmation of that exact mutation list.

Lead with a compact comparison so the user can distinguish facts from proposals:

| Area | Current state | Recommended state | Proposed change |
| --- | --- | --- | --- |
| Project / workflow / Labels | Observed facts | Desired convention | Minimum action or `None` |

Do not perform any Project, Issue, PR, Workflow, automation, or Label mutation until the user
explicitly confirms the listed minimum changes.
