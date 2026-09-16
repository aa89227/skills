---
name: github-project-workflow
description: |
  Operate a GitHub Issue, Project, pull request, branch, milestone, and release as one
  deterministic development workflow for feature, bug, improvement, refactor, chore, and
  documentation work. Use when an agent must create, resume, implement, review, integrate,
  hand off, or release a GitHub-tracked requirement or initiative. Do not use for unrelated local work with
  no GitHub-tracked requirement.
license: MIT
metadata:
  author: aa89227
  version: "1.2"
  tags: ["github", "project", "issue", "pull-request", "epic", "roadmap", "priority", "workflow", "checkpoint", "release"]
---

# GitHub Project Development Workflow

## Purpose

Use this skill to make a GitHub Issue recoverable across agent sessions and to keep requirement,
review, integration, lifecycle, and release facts in their designated sources of truth. The
workflow supports one PR, multiple PRs, stacked PRs, milestones, releases, and human handoff.

This skill is applicable when work is represented by a GitHub Issue and Project. For a purely
local task with no tracked requirement, do not invent a Project lifecycle around it.

## Operating contract

- MUST read the current GitHub artifacts and repository state; MUST NOT rely on previous chat
  context as a handoff record.
- MUST keep the Issue body as the current canonical specification. Comments are history, not a
  required specification source.
- MUST classify a request as one Requirement or an Initiative with child Requirements using
  [references/epic.md](references/epic.md). The user does not need to manage that decomposition.
- MUST write Issue requirements in plain, non-engineering language by default. When technical
  detail is necessary, define the term for the intended reader and keep implementation detail in
  the final Issue section as specified by [references/issue.md](references/issue.md).
- MUST treat Issue Priority as planning metadata, not a lifecycle status, review result, severity
  score, or roadmap horizon; use only the values and rules in [references/priority.md](references/priority.md).
- MUST record a reason and supporting evidence whenever Priority is assigned or changed. Do not
  raise Priority for a speculative security concern, a category label, or elapsed time alone.
- MUST keep a review finding's `Blocking`, `Non-blocking`, or `Informational` classification
  separate from the Issue's Priority.
- MUST use only the lifecycle statuses defined below. `Blocked`, `CI Failed`, `Changes Requested`,
  and `Waiting` are metadata or signals, never lifecycle statuses.
- MUST run the transition protocol in
  [references/lifecycle.md](references/lifecycle.md) before changing Project Status.
- MUST emit a `Workflow Checkpoint` after every lifecycle status change and at session end; MUST
  follow [references/checkpoint.md](references/checkpoint.md) for the format and hard-pause rules.
- MUST stop the transition and report the missing condition when an entry criterion, exit
  criterion, allowed transition, or required human approval is absent.
- MUST preserve unknown local changes. MUST NOT reset, clean, restore, overwrite, or destructively
  check out them to make the workspace convenient.

## Sources of truth

| Artifact | Authoritative responsibility |
| --- | --- |
| Issue body | Requirement: problem, goal, scope, criteria, constraints, edge cases, open questions, and release note |
| Parent Issue/sub-issues | Initiative hierarchy and aggregate planning context |
| Pull request(s) | Review: implementation diff, CI, review discussion/history, and stack relationships |
| Remote target branch | Integration: what is actually integrated |
| GitHub Project Status | Lifecycle of the whole Issue, never one PR |
| GitHub Project Priority or Issue Workflow Metadata | Current action order for the whole Issue; its reason and evidence remain in the Issue body |
| GitHub Milestone | Planned release version |
| Git release tag | Exact commit selected as a release target |
| GitHub Release | Published version |
| Issue Release Note | User-facing change description |

## Priority

Priority expresses the current action order for an Issue. It does not describe technical severity,
replace a review finding classification, move lifecycle Status, authorize implementation or merge,
or substitute for a Milestone or roadmap field. Use [references/priority.md](references/priority.md)
when assigning or reassessing it.

## Lifecycle

The only valid lifecycle statuses are:

`Inbox` → `Specifying` → `Ready` → `In Progress` → `In Review` → `Ready to Merge` → `Done`

The following rework transitions are also valid:

- `Ready` → `Specifying`
- `In Progress` → `Specifying`
- `In Review` → `In Progress`
- `In Review` → `Specifying`
- `Ready to Merge` → `In Progress`
- `Ready to Merge` → `Specifying`
- any non-terminal status → `Cancelled`, only on an explicit human decision

`Done` and `Cancelled` are terminal for this workflow. An agent MUST NOT invent another status,
reopen a terminal Issue, or reuse it for new post-terminal work; create a new linked Issue instead.
Read the full matrix and criteria in
[references/lifecycle.md](references/lifecycle.md).

For large or uncertain product requests, `Discussion`, `Research`, `Prototype`, and `Delivery` are
work-mode metadata, not lifecycle statuses. Read [references/epic.md](references/epic.md); do not
create lifecycle statuses for those modes.

## Human approval gates

The following are the workflow's human approval gates:

- `Specifying` → `Ready`: the current specification is approved.
- `In Review` → `Ready to Merge`: the current implementation is approved. For a
  `Research`/`Prototype` item with no repository artifact, the human may approve the recorded
  result by directly setting its Project Status to `Ready to Merge`.

Silence, lack of comments, passing CI, passing tests, an agent's own review, another agent's
opinion, an existing PR, or completed code MUST NOT be treated as approval. For this workflow,
specification approval is a human Project Status change to `Ready`; Delivery implementation
approval is a current native GitHub `APPROVED` review on every required PR; and no-artifact
discovery approval is a human Project Status change to `Ready to Merge` after the recorded result
is reviewable. The human action/review must be verifiable and apply to the current specification
or implementation/result. `Cancelled` is not an approval gate, but it still requires a separate
explicit human cancellation decision as defined in [references/lifecycle.md](references/lifecycle.md).
For an `Initiative` parent, child approvals and accepted discovery results satisfy the aggregate
gate; the parent has no implementation PR of its own.

## Session entry and exit

At session start, perform the minimum context recovery in
[references/session-protocol.md](references/session-protocol.md): read the Issue, Project metadata,
related PRs, target/base branches, working tree, and remote state; then reconcile facts before
acting. If facts conflict, investigate the objective state first; do not edit Status merely to make
the records look consistent.

At session end, update the canonical GitHub artifacts with the latest specification, status, PR
description, pushed progress, verification, blockers, and stack relationship. Do not create a
separate handoff file or force an unfinished task into review. If Delivery implementation has
started but is incomplete, keep the Issue `In Progress` with a Draft PR. If no-artifact discovery
work has started but is incomplete, keep it `In Progress` and record the next evidence or
experiment; if work has not started, keep the truthful earlier status.

## Reference routing

Read only the references needed for the current operation:

- Lifecycle transition, status criteria, approval, or cancellation: [lifecycle.md](references/lifecycle.md)
- Checkpoint output, pause/resume behavior, or next-action reporting: [checkpoint.md](references/checkpoint.md)
- Decide whether to decompose a broad/uncertain request or manage discovery children: [epic.md](references/epic.md)
- Create or revise an Issue, type, acceptance criteria, open questions, or release note: [issue.md](references/issue.md)
- Assign or reassess Issue Priority and record its rationale: [priority.md](references/priority.md)
- Create/revise/review/stack PRs or determine review readiness: [pull-request.md](references/pull-request.md)
- Branches, commits, rebase, force-push, merge, local integration, or unknown changes: [git.md](references/git.md)
- Takeover, reconciliation, or session handoff: [session-protocol.md](references/session-protocol.md)
- Milestone planning, eligibility, changelog, or GitHub Release: [release.md](references/release.md)
- Short/medium/long planning, Roadmap view, dates, horizon, or confidence: [roadmap.md](references/roadmap.md)

Do not load every reference for a simple operation. When an operation crosses domains, read the
smallest set that covers all affected sources of truth.

## Non-negotiable safety rules

- MUST NOT implement before `Specifying` → `Ready` has human approval.
- MUST NOT merge or integrate before `In Review` → `Ready to Merge` has human approval.
- MUST NOT push implementation commits to the target branch before that implementation approval.
- MUST prefer GitHub PR merge. Local integration is a controlled fallback only, and must follow
  [references/git.md](references/git.md) and reconcile every affected PR honestly.
- MUST verify the remote target branch before setting `Done` for a Delivery Issue or any discovery
  item with a repository artifact. For a Research/Prototype item with no artifact, MUST verify the
  accepted findings are durable in the child and parent Issues; a GitHub PR labeled Merged alone is
  insufficient in either case.
- MUST NOT force-push the target branch, split an Issue merely because implementation is large,
  create an Initiative without the triggers in [references/epic.md](references/epic.md), use PR
  closing keywords, or fabricate a user-facing release note.
- MUST use semantic branch names and the repository's actual target branch; `main` is never an
  assumed target.
