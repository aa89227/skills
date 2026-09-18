# Session Start, Handoff, and Reconciliation

Use this reference whenever an agent starts work on an existing Issue, takes over another agent's
session, or ends a session with work still in progress. GitHub artifacts are the handoff system;
do not create a separate `HANDOFF.md` or rely on conversation history.

## Start protocol

The agent MUST perform context recovery before implementation or a lifecycle transition.

If the Issue is part of an Initiative, also recover the parent Issue, native parent/sub-issue
relationship, sibling child Issues, each child's `Issue Role` and `Work Mode`, and the aggregate
completion state. Do not assume the parent or the newest child represents the complete work.

### 1. Recover the requirement

Read the Primary Issue's:

- body and latest canonical specification;
- GitHub open/closed state;
- Project Status;
- Type;
- Project Priority and its Issue-body rationale;
- Milestone, including `None`;
- acceptance criteria;
- constraints and edge cases;
- open questions and their blocking state; and
- Release Note; and
- selected Issue language, Issue Role, Parent Issue, Work Mode, Approval Source, and Review Mode from
  `Workflow Metadata`.

If the Issue body is stale, incomplete, or contradicts current human decisions, update the body or
stop for clarification before implementation. Do not reconstruct the requirement from comments
alone.

### 2. Recover workflow metadata

Read Project Status, Project Priority, `Blocked`, `Blocked By`, `Unblocking Condition`, and other
repository-defined metadata relevant to the Issue. For roadmap-managed work, also recover `Roadmap Horizon`,
`Roadmap Confidence`, `Start Date`, `Target Date`, and `Next Milestone` when those fields exist.
Confirm that Project Status describes the Issue rather than one PR.

### 3. Recover implementation and review

Find every related PR using the exact `Primary Issue: #<number>` convention and repository/branch
relationship. For each PR, read:

- Draft or Ready state;
- selected Review Mode, human reviews or solo-maintainer review records, and approval validity;
- CI/check results;
- base and head branches;
- stack dependency and ordering; and
- summary and verification in the PR body.

Do not assume the newest or top PR is the complete implementation.

### 4. Recover repository state

Inspect the repository default branch, intended target branch, current branch, worktree status,
remote tracking, remote target head, and relevant remote branches. Inspect unknown local changes
before any command that could overwrite them. Follow [git.md](git.md).

### 5. Reconcile before acting

Compare these four dimensions:

| Dimension | Question |
| --- | --- |
| Project | What lifecycle status, Priority, and blocker metadata are recorded? |
| Issue | Is the requirement body, Priority rationale, and Issue open/closed state current? |
| PRs | What is reviewed, approved, merged, closed, draft, or failing? |
| Remote repository | What content is actually in the target branch? |

If they disagree, establish objective facts first. Do not change Project Status merely to make it
match a PR label, Issue closed state, or local branch. Record the inconsistency and choose the
allowed transition only after the evidence and approval conditions are satisfied.

## Required reconciliation cases

- `Project = In Progress` while code exists in the target branch: verify whether valid human
  approval under the selected Review Mode existed, how integration occurred, which commit/tree is
  present, whether criteria pass, and
  whether the push was authorized. If all required review history, persisted approval, target
  content, and final verification exist, replay the allowed historical path through `In Review` →
  `Ready to Merge` → `Done`; never use a direct `In Progress` → `Done` shortcut. Otherwise do not
  set `Done` and report the missing condition.
- PR says `Merged` but the remote target branch does not contain the expected result: treat the
  integration as unverified and investigate base/head/target state before any further merge.
- Issue is closed while Project Status is active: treat Issue closure as an inconsistency, not as
  proof of `Done`. Do not reopen or change Status without repository policy or explicit human
  direction.
- A required stack PR is missing, closed without integration, or points to the wrong base: keep the
  Issue from review/done, repair the relationship if safe, or report the missing condition.

## Session-end protocol

Before ending a session, the agent MUST ensure:

- the Issue body is the latest canonical specification;
- Project Status, Priority, Priority rationale, and blocker metadata reflect verified facts;
- every PR description has the correct Primary Issue, summary, verification, and stack context;
- necessary implementation progress is pushed to the implementation branch;
- verification results and known limitations are recorded;
- blocker reason and unblocking condition are recorded when blocked; and
- stack base/head relationships are correct; and
- a `Workflow Checkpoint` is emitted according to [checkpoint.md](checkpoint.md).

If Delivery implementation has begun but is incomplete, keep `In Progress` and a Draft PR only
when the applicable Design System gate was already `Open`. If a Design System gate is still
`Blocked`, keep the Issue at its truthful pre-implementation Status, record the missing handoff
condition, and do not create or retain a runtime implementation PR. If Research/Prototype work
without a repository artifact has begun but is incomplete, keep `In Progress` and record the next
evidence or experiment; no PR is required. If the work is still specifying, keep `Specifying`; if
it is approved but not started, keep `Ready`. Session end does not justify `In Review`, `Ready to
Merge`, `Done`, or `Cancelled`.

Do not create a verbose session summary. The Issue body, Project metadata, PRs, branches, and
verification records must contain the durable handoff information.

## Failure and partial completion

If a remote mutation succeeds but a later metadata update fails, preserve the objective fact,
record the failed synchronization, and reconcile on the next session. Do not repeat an integration
because a status or comment was not updated. If a required fact cannot be verified, leave the
lifecycle where it is and report the exact missing condition.
