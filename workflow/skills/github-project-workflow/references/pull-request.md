# Pull Request and Stack Specification

Use this reference when creating, updating, reviewing, or integrating implementation PRs. A PR
is the review source of truth; the Project Status still describes the whole Issue.

## Primary Issue contract

Every implementation PR MUST identify exactly one Primary Issue with this line:

```text
Primary Issue: #123
```

Replace `#123` with the actual Issue number. Do not use `Closes #123`, `Fixes #123`, or
`Resolves #123`; Issue closure is controlled by the lifecycle workflow. An implementation PR MAY
mention non-primary context, but it MUST NOT create ambiguity about the one Primary Issue or enable
automatic closure.

## PR body

Keep the body concise and use this structure. Do not copy the complete Issue specification into a
PR.

```markdown
## Issue

Primary Issue: #123

## Summary

<!-- What this PR implements in the Issue's scope. -->

## Verification

- <command/check and result>

## Stack

- Stack: Single
- Previous: None
- Next: None
```

For a stack, replace the Stack section with human-readable context such as `Stack: 2/3`,
`Previous: #201`, and `Next: #203`. This context is not the dependency source of truth; the
GitHub-native base/head branch relationship is authoritative.

## Creation and review

- Every implementation PR MUST start as Draft unless the repository does not support Draft PRs or
  a human explicitly requests immediate review.
- A Draft PR MAY exist while the Issue is `Ready` for planning, but actual implementation starts
  the Issue's `In Progress` state.
- A PR becoming Ready for Review MUST NOT by itself move the Issue to `In Review`.
- Move the Issue to `In Review` only when all required implementation slices and PRs are reviewable,
  required checks pass, known limitations are recorded, and the Issue release note matches the
  actual result.
- Human approval for `In Review` → `Ready to Merge` MUST be the native GitHub `APPROVED` review
  state on every required PR in the Issue, including every required stack slice. One approved PR
  does not approve an incomplete stack.
- Review comments, requested changes, CI failure, and test failure are signals. They do not create
  lifecycle statuses. Implementation work routes to `In Progress`; a material requirement change
  routes to `Specifying`.

The agent MUST NOT self-approve, merge before human implementation approval, or infer approval from
silence, passing checks, or another agent.

## Multiple and stacked PRs

Use multiple or stacked PRs when one logical Issue is large or when reviewable slices improve
integration. All PRs for the same requirement MUST use the same `Primary Issue` number. Each stack
PR MUST have its own semantic branch and a correct GitHub base/head relationship.

The dependency order is determined by the actual branch relationship and GitHub PR base/head data.
The PR description's `Stack: n/m`, `Previous`, and `Next` values are only human-readable context and
MUST be corrected when relationships change.

Before review, verify that every required slice is present, reviewable, and tested in the intended
combination. Before `Done`, integrate the stack in dependency order or use an approved controlled
fallback, then verify the final target branch—not merely the top PR's status.

## PR state reconciliation

When a PR is merged, closed, draft, or approved, compare that fact with the Issue's Project Status
and the remote target branch. A GitHub PR `Merged` state proves only that GitHub recorded a merge;
it does not prove that the whole Issue is integrated or that acceptance criteria are met. Follow
[lifecycle.md](lifecycle.md) and [git.md](git.md) before advancing the Issue.
