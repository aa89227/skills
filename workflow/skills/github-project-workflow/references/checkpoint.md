# Workflow Checkpoint and Pause Specification

Use this reference whenever a lifecycle status changes, a session ends, a transition is paused, or
the agent returns control to Human. This file defines user-facing progress reporting and pause
behavior; it does not add lifecycle statuses or change the transition matrix.

## Checkpoint obligation

After every successful lifecycle status change, after reconciling a stale or inconsistent status,
and before ending a session, the agent MUST emit one compact `Workflow Checkpoint`. The agent MUST
also emit a checkpoint before pausing for a missing condition or human decision.

The checkpoint MUST use the Issue's selected language for prose. Lifecycle values, Work Mode
values, exact GitHub actions, Issue numbers, PR numbers, branch names, commands, and other
machine-readable tokens MUST remain unchanged.

Use this shape:

```text
Workflow Checkpoint

Issue: #123
Role: Requirement
Work Mode: Delivery
Lifecycle Status: In Review
Execution: Waiting for Human
Completed: <concise completed work>
Verification: <checks, review, or objective facts>
Next Action: Human: <one concrete next action>
Waiting For: <Human action, information, setup, or None>
Resume Condition: <observable condition, or None when terminal>
```

`Execution` is a reporting value, not a lifecycle status. It MUST be one of:

- `Continuing`: the agent may continue an allowed operation without waiting for Human input.
- `Waiting for Human`: a human approval, cancellation, clarification, or decision is required.
- `Waiting for Information`: an external fact, permission, setup, or unverified state is missing.
- `Terminal`: the Issue is `Done` or `Cancelled`; no next action exists in this workflow.

`Next Action` MUST identify the actor. Use `Agent:` when the agent can continue autonomously and
`Human:` when the workflow is paused. `Waiting For` MUST be `None` when `Execution: Continuing`.
`Resume Condition` MUST describe an observable GitHub, repository, or human action; do not write
vague text such as `when ready`.

The checkpoint SHOULD fit on one screen. The agent MUST report facts and the next action, not a
verbose session summary. Detailed history belongs in the Issue, PR, Project metadata, or Git
history according to their source-of-truth responsibilities.

## Hard-pause conditions

The agent MUST set `Execution: Waiting for Human` or `Waiting for Information` and stop the
affected operation when any of these conditions applies:

1. `Specifying` → `Ready` needs current specification approval. Tell Human to change the Issue's
   Project Status to `Ready` after reviewing the canonical Issue body.
2. `In Review` → `Ready to Merge` needs implementation approval. In `Collaborative` mode, tell
   Human to submit a native GitHub `APPROVED` review on every required artifact PR. In `Solo
   Maintainer` mode, tell Human to review the current head of every required PR, record the full
   SHA and completed human review, then directly set Project Status to `Ready to Merge`. For a
   Research/Prototype item with no repository artifact, tell Human to set its Project Status to
   `Ready to Merge` after reviewing the recorded result.
3. A material requirement change is identified but the new behavior, scope, acceptance criteria,
   or important constraint is not explicit. Return the affected Issue to `Specifying`, update the
   canonical body with the known facts, and wait for clarification and new specification approval.
4. Human cancellation, reparenting, replacement, or removal of Initiative work is required.
5. A lifecycle transition is not in the allowed matrix, an entry/exit criterion is missing, or
   the configured Project does not expose the required lifecycle values.
6. Project, Issue, PR, or remote repository facts conflict and authorization or integration cannot
   be established. Do not change Status merely to hide the conflict.
7. Unknown local changes could be overwritten, or a required Git operation would be destructive.
8. A required permission, Project setup, native parent/sub-issue relationship, Roadmap field, or
   external fact is unavailable.
9. A release version, exact Milestone, tag target, or explicit `Next Milestone` is required but
   has not been supplied or verified.

When pausing, the agent MUST state the exact missing condition and the action that will satisfy it.
The agent MUST NOT continue implementation, integration, lifecycle advancement, or release work
that depends on the missing condition.

## Autonomous continuation

The agent MAY continue without waiting for a chat reply when the transition protocol and all
criteria permit an agent transition, including:

- `Inbox` → `Specifying`;
- `Ready` → `In Progress` when approved work actually starts;
- `In Progress` → `In Review` when the complete Requirement or discovery result is reviewable;
- `In Review` → `In Progress` for implementation feedback or invalidated readiness;
- `Ready to Merge` → `Done` after approved integration and final verification; and
- ordinary Issue, PR, branch, test, metadata, and reconciliation work that does not hit a
  hard-pause condition.

After an autonomous transition, emit the checkpoint and continue if the requested operation is
still in scope. A checkpoint is a progress report, not an implicit request for confirmation.

The agent MUST NOT treat a chat message such as `approved`, `開始實作`, or `merge` as a replacement
for the configured GitHub approval evidence. A chat instruction MAY authorize an otherwise allowed
operation, but `Specifying` → `Ready` still requires the human Project Status change and
repository-artifact implementation approval still requires either current native `APPROVED` reviews
in `Collaborative` mode or the documented human status acceptance in `Solo Maintainer` mode.

## Status-specific next actions

When no hard-pause condition overrides the normal path, use the following deterministic guidance:

| Lifecycle Status | Normal checkpoint next action |
| --- | --- |
| `Inbox` | `Agent: organize the Issue and move to Specifying when requirement work starts.` |
| `Specifying` | `Agent: update the canonical specification`; when criteria are complete, `Human: set Project Status to Ready`. |
| `Ready` | `Agent: begin the approved Work Mode`; change to `In Progress` only when work actually starts. |
| `In Progress` | `Agent: continue the approved implementation or discovery work and prepare the complete result for review.` |
| `In Review` | `Human: review the complete result`; then provide the required approval for the selected Review Mode or discovery acceptance. |
| `Ready to Merge` | `Agent: verify approval, checks, target branch, and integrate using the GitHub merge policy.` |
| `Done` | `None`; terminal. Close the Issue if not already closed. |
| `Cancelled` | `None`; terminal. Preserve the cancellation reason and history. |

For an Initiative parent, describe the aggregate child condition in `Completed`, `Verification`,
and `Next Action`; do not report one child as if it were the whole parent. For `Discussion`,
`Research`, and `Prototype`, name the exact question, evidence, experiment, decision, or human
acceptance still required.

## Session-end checkpoint

At session end, the agent MUST emit a checkpoint even when no lifecycle status changed. It MUST
state the truthful current status and the next resumable action. The agent MUST NOT advance to
`In Review`, `Ready to Merge`, or `Done` merely to produce a cleaner checkpoint.
