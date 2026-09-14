---
name: github-project-workflow
description: |
  Operate a GitHub Issue, Project, pull request, branch, milestone, and release as one
  deterministic development workflow for feature, bug, improvement, refactor, chore, and
  documentation work. Use when an agent must create, resume, implement, review, integrate,
  hand off, or release a GitHub-tracked requirement. Do not use for unrelated local work with
  no GitHub-tracked requirement.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["github", "project", "issue", "pull-request", "workflow", "release"]
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
- MUST write Issue requirements in plain, non-engineering language by default. When technical
  detail is necessary, define the term for the intended reader and keep implementation detail in
  the final Issue section as specified by [references/issue.md](references/issue.md).
- MUST use only the lifecycle statuses defined below. `Blocked`, `CI Failed`, `Changes Requested`,
  and `Waiting` are metadata or signals, never lifecycle statuses.
- MUST run the transition protocol in
  [references/lifecycle.md](references/lifecycle.md) before changing Project Status.
- MUST stop the transition and report the missing condition when an entry criterion, exit
  criterion, allowed transition, or required human approval is absent.
- MUST preserve unknown local changes. MUST NOT reset, clean, restore, overwrite, or destructively
  check out them to make the workspace convenient.

## Sources of truth

| Artifact | Authoritative responsibility |
| --- | --- |
| Issue body | Requirement: problem, goal, scope, criteria, constraints, edge cases, open questions, and release note |
| Pull request(s) | Review: implementation diff, CI, review discussion/history, and stack relationships |
| Remote target branch | Integration: what is actually integrated |
| GitHub Project Status | Lifecycle of the whole Issue, never one PR |
| GitHub Milestone | Planned release version |
| Git release tag | Exact commit selected as a release target |
| GitHub Release | Published version |
| Issue Release Note | User-facing change description |

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

## Human approval gates

Only these transitions require explicit human approval:

- `Specifying` → `Ready`: the current specification is approved.
- `In Review` → `Ready to Merge`: the current implementation is approved.

Silence, lack of comments, passing CI, passing tests, an agent's own review, another agent's
opinion, an existing PR, or completed code MUST NOT be treated as approval. For this workflow,
specification approval is a human Project Status change to `Ready`; implementation approval is a
current native GitHub `APPROVED` review on every required PR. The human action/review must be
verifiable and apply to the current specification or implementation.

## Session entry and exit

At session start, perform the minimum context recovery in
[references/session-protocol.md](references/session-protocol.md): read the Issue, Project metadata,
related PRs, target/base branches, working tree, and remote state; then reconcile facts before
acting. If facts conflict, investigate the objective state first; do not edit Status merely to make
the records look consistent.

At session end, update the canonical GitHub artifacts with the latest specification, status, PR
description, pushed progress, verification, blockers, and stack relationship. Do not create a
separate handoff file or force an unfinished task into review. If implementation has started but
is incomplete, keep the Issue `In Progress` with a Draft PR; if implementation has not started,
keep the truthful earlier status.

## Reference routing

Read only the references needed for the current operation:

- Lifecycle transition, status criteria, approval, or cancellation: [lifecycle.md](references/lifecycle.md)
- Create or revise an Issue, type, acceptance criteria, open questions, or release note: [issue.md](references/issue.md)
- Create/revise/review/stack PRs or determine review readiness: [pull-request.md](references/pull-request.md)
- Branches, commits, rebase, force-push, merge, local integration, or unknown changes: [git.md](references/git.md)
- Takeover, reconciliation, or session handoff: [session-protocol.md](references/session-protocol.md)
- Milestone planning, eligibility, changelog, or GitHub Release: [release.md](references/release.md)

Do not load every reference for a simple operation. When an operation crosses domains, read the
smallest set that covers all affected sources of truth.

## Non-negotiable safety rules

- MUST NOT implement before `Specifying` → `Ready` has human approval.
- MUST NOT merge or integrate before `In Review` → `Ready to Merge` has human approval.
- MUST NOT push implementation commits to the target branch before that implementation approval.
- MUST prefer GitHub PR merge. Local integration is a controlled fallback only, and must follow
  [references/git.md](references/git.md) and reconcile every affected PR honestly.
- MUST verify the remote target branch before setting `Done`; a GitHub PR labeled Merged alone is
  insufficient.
- MUST NOT force-push the target branch, split an Issue merely because implementation is large,
  use PR closing keywords, or fabricate a user-facing release note.
- MUST use semantic branch names and the repository's actual target branch; `main` is never an
  assumed target.
