# Lifecycle and Transition Specification

This file is the normative specification for GitHub Project Status. A status is attached to the
whole Issue, not to an individual PR. The terms MUST, MUST NOT, SHOULD, SHOULD NOT, and MAY are
normative.

## Status vocabulary

The complete and closed lifecycle vocabulary is:

`Inbox`, `Specifying`, `Ready`, `In Progress`, `In Review`, `Ready to Merge`, `Done`, `Cancelled`.

An agent MUST NOT create or select `Blocked`, `CI Failed`, `Changes Requested`, `Waiting`, or any
other lifecycle value. Use orthogonal metadata for those conditions:

| Field | Required meaning |
| --- | --- |
| `Blocked` | `Yes` or `No`; does not change lifecycle status |
| `Blocked By` | Person, system, decision, or dependency causing the block; use `None` when unblocked |
| `Unblocking Condition` | Observable condition that removes the block; use `None` when unblocked |

Use existing Project fields with these names when they exist. If they do not exist, keep the fields
in the Issue's `Workflow Metadata` section, and mirror them to comments only as history. A block
does not authorize a status change; when unblocked, retain the prior lifecycle status.

If the configured Project does not expose the exact lifecycle values, the agent MUST report a
workflow-setup blocker and MUST NOT create, rename, or silently map a lifecycle value during the
task.

## Definitions

- **Material requirement change** changes expected behavior, acceptance criteria, scope, a
  user-visible behavior, or an important constraint. A new implementation detail is not a
  material requirement change.
- **Implementation approval** is explicit human authorization for the current implementation,
  including all required PRs in a multi-PR Issue. A bot review or agent statement is not approval.
- **Required implementation** is every code, test, documentation, configuration, and migration
  change needed to satisfy the current acceptance criteria. A large implementation MAY use
  multiple PRs without creating multiple Issues.
- **Mechanical integration adjustment** changes commit/branch mechanics or resolves a conflict
  without changing approved behavior. If behavior changes, it is implementation work.

## Allowed transition matrix

No transition outside this matrix is valid. The agent may perform a transition only when the actor,
reason, exit criteria, and entry criteria in the row are all satisfied.

| From | To | Actor | Required condition |
| --- | --- | --- | --- |
| `Inbox` | `Specifying` | Agent | The Issue exists and requirement work has started; no implementation is performed. |
| `Specifying` | `Ready` | Human action required | Required specification fields are current, scope and acceptance criteria are clear, no blocking open question remains, and a human directly changes Project Status to `Ready`. The agent only verifies this evidence. |
| `Ready` | `In Progress` | Agent | The approved specification is frozen and implementation has actually started, such as code/test work or an implementation commit. |
| `Ready` | `Specifying` | Agent after an identified material requirement change | Update the Issue body and invalidate the prior specification approval. |
| `In Progress` | `In Review` | Agent | All required implementation is complete, required checks pass, all required PRs exist and are reviewable, limitations are recorded, and the release note matches the result. During reconciliation, persisted review history for an already processed PR MAY satisfy the reviewable condition. |
| `In Progress` | `Specifying` | Agent after an identified material requirement change | Update the Issue body before further implementation; do not preserve an obsolete approval. |
| `In Review` | `In Progress` | Agent | Human or CI feedback requires implementation work, or current approval/readiness is invalidated. Record feedback and the next verification. |
| `In Review` | `Specifying` | Agent after an identified material requirement change | The feedback changes behavior, acceptance criteria, scope, or an important constraint. Update the Issue body. |
| `In Review` | `Ready to Merge` | Agent after human PR approval | Every required PR has a current native GitHub `APPROVED` review from a human, all required PRs are covered, and no material change is pending. |
| `Ready to Merge` | `In Progress` | Agent | A material implementation change is required, approval is no longer valid, or integration reveals a behavior change that must be implemented. Re-verify and re-review. |
| `Ready to Merge` | `Specifying` | Agent after an identified material requirement change | Integration or review reveals that the requirement itself must change. Update the Issue body and obtain new specification approval. |
| `Ready to Merge` | `Done` | Agent | Approved implementation is integrated into the intended remote target branch and all Done criteria below are true. |
| any non-terminal status | `Cancelled` | Human decision required | A human explicitly decides that the requirement will not proceed. Record the reason and preserve the history. |

`Done` and `Cancelled` have no outgoing transitions in this workflow. Work requested after a
terminal state MUST use a new linked Issue. The agent MUST NOT reopen or reuse the terminal Issue.

## Status criteria

### Inbox

`Inbox` means the requirement exists but has not been formally organized. The agent MAY create the
Issue, add it to the Project, set type and milestone metadata, record initial intent, and begin
discussion. The agent MUST NOT implement. Move to `Specifying` when requirement organization starts.

### Specifying

The Issue body MUST be the current specification and contain the required headings in
[issue.md](issue.md). Comments MAY preserve discussion history, but the current requirement MUST
be understandable from the body alone. The agent MAY research, ask questions, analyze constraints,
and propose criteria; it MUST NOT approve the specification or implement it.

The exit condition for `Ready` is: clear acceptance criteria, clear scope and out-of-scope, no
unresolved blocking open question, required constraints/edge cases captured, and explicit human
approval of the current body.

### Ready

`Ready` means the specification is human-approved and frozen for implementation. The agent MAY
plan, create a semantic branch, and create a Draft PR, but MUST NOT start implementation until it
transitions to `In Progress`. A material requirement change returns to `Specifying`.

### In Progress

`In Progress` means implementation is underway. The agent MAY code, commit, push implementation
branches, create or update Draft PRs, test, document, and address review feedback. Every
implementation PR MUST start as Draft unless the repository does not support Draft PRs or a human
explicitly requests immediate review. A failed check is a signal, not a new lifecycle status.

Move to `In Review` only when the whole Issue—not merely one PR—is reviewable.

### In Review

`In Review` means every required implementation slice is ready for human review. The agent MAY
answer questions and make review-driven changes, but MUST NOT self-approve or infer approval from
silence, CI, tests, or another agent. Implementation feedback returns to `In Progress`; a material
requirement change returns to `Specifying`. Human approval of the current implementation permits
`Ready to Merge`.

### Ready to Merge

`Ready to Merge` means the current implementation has human approval and is eligible for
integration. No material implementation change may be introduced here. A mechanical rebase or
conflict resolution MAY remain in this status only when approved behavior is preserved and the
approval remains valid. If the platform dismisses approval or behavior changes, use
`Ready to Merge` → `In Progress` and repeat review.

### Done

The agent MUST NOT set `Done` until all of the following are true:

1. Current human implementation approval exists.
2. Every required PR or stack slice has been integrated or honestly reconciled as locally
   integrated.
3. The remote intended target branch contains the complete approved result.
4. Final verification passes against the integrated result.
5. Acceptance criteria are satisfied.
6. No required implementation remains and no required PR is left unprocessed.
7. The Issue type, milestone, and release note accurately describe the result.

After setting `Done`, close the Issue. If a metadata update fails after integration, record the
objective result and reconcile later; do not integrate again merely because Project metadata is
temporarily stale.

### Cancelled

`Cancelled` means the requirement will not proceed. An agent MUST NOT cancel because work is hard,
CI fails, tests fail, merge conflicts occur, or implementation is complex. A human decision and a
recorded reason are required. Close the Issue after the status is recorded.

## Approval validity

Specification approval is valid only when a verifiable human directly changed Project Status from
`Specifying` to `Ready` for the current Issue body. Implementation approval is valid only when every
required PR has a verifiable human native GitHub review state of `APPROVED` for its current head.
Chat-only statements, comments without the native approval state, unidentifiable actors, or stale
reviews are insufficient. The agent MUST treat approval as invalid after a material specification
or implementation change. A mechanical rebase may retain approval only when the resulting
behavior/diff is demonstrably equivalent and the hosting platform still considers the approval
valid. If either condition fails, return to `In Progress` and obtain review again.

## Transition protocol

Before every lifecycle status change, the agent MUST:

1. Read the Issue body, Issue open/closed state, and current Project Status.
2. Read relevant PR state, reviews, checks, base/head, and stack relationship.
3. Read the remote target branch when the transition concerns integration, `Done`, or
   reconciliation.
4. Determine the intended transition and its reason.
5. Verify that the transition is in the allowed matrix.
6. Verify the current status exit criteria.
7. Verify the target status entry criteria and human gate, if any.
8. Execute the transition.
9. Synchronize affected metadata, PR descriptions, Issue state, and blocker fields.
10. Record any missing condition instead of transitioning.

Reading only a PR or only a Project card is insufficient evidence for a lifecycle transition.

## Stale-status reconciliation

If remote facts show that an Issue already passed through an allowed lifecycle step while Project
metadata was not synchronized, the agent MUST NOT invent a shortcut such as `In Progress` → `Done`.
The agent MAY replay the missing allowed transitions in order when evidence exists for every step:

1. Required implementation and checks are complete.
2. PR review history proves the implementation was reviewable.
3. Persisted human implementation approval covers the current implementation.
4. The remote target branch contains the approved result.
5. Acceptance criteria and final verification pass.

When these facts are present, reconcile through `In Review` → `Ready to Merge` → `Done` using the
ordinary protocol. When any fact is missing—especially approval or authorization for target-branch
content—retain the current status and report the exact missing condition.
