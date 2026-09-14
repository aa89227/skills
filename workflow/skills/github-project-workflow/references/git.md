# Git, Branch, and Integration Specification

Use this reference for branch creation, commits, rebases, force-pushes, local changes, merge, and
remote integration. The remote target branch is the integration source of truth.

## Target branch

The target branch MUST be determined from repository and PR evidence. The agent MUST inspect the
repository default branch and the relevant PR base branch; it MUST NOT assume `main`. For a new
Issue, use the repository's configured target/default branch only when no explicit project or
release target exists, and record the chosen target in the PR context.

The target branch is the branch whose remote content must contain the complete approved result. A
local branch, a PR label, or a merge button state is not sufficient evidence.

## Branch naming

Use:

```text
<type>/<issue-number>-<short-description>
```

Examples: `feat/123-oauth-login`, `fix/456-token-refresh`, and
`refactor/789-auth-service`.

Every stack slice MUST have its own branch with a semantic description of that slice. Do not use
`part-1`, `part-2`, or `part-3` when a meaningful slice name exists. A branch name does not replace
the GitHub base/head relationship.

## Commits and history

Commit messages MUST describe the change, for example `Add OAuth callback handler`. Conventional
Commits are not required. Avoid meaningless messages such as `update`, `changes`, or `stuff`.
Use reasonable commit granularity; do not impose a fixed commit count.

Release notes MUST NOT be generated from commit history.

Implementation branches MAY be rebased. After review, avoid unnecessary history rewrites. A stack
dependency MAY require a rebase and force update; use `--force-with-lease` only on an implementation
branch and only after confirming the remote lease. NEVER force-push the target branch, including
with `--force-with-lease`.

## GitHub merge policy

The agent MUST prefer GitHub Pull Request Merge. The normal path is:

```text
Ready to Merge → verify checks → GitHub PR merge → verify remote target branch → Done
```

Local integration is a controlled fallback only when at least one of these applies:

- a stacked PR needs special integration;
- a special rebase or merge structure is required;
- a complex conflict cannot be represented by GitHub's merge operation;
- GitHub merge cannot produce the required commit structure; or
- repository policy explicitly requires local integration.

Convenience alone is not a valid reason for local integration.

## Local integration fallback

Before local integration, the Issue MUST be `Ready to Merge` and current human implementation
approval MUST still be valid. The agent MUST perform all of the following:

1. Inspect the worktree and preserve unknown local changes.
2. Fetch/refresh the remote state and update the local target branch without discarding work.
3. Confirm the remote target head and the approved PR/stack commits.
4. Integrate with the required merge, rebase, or squash strategy.
5. Resolve conflicts without changing approved behavior. If behavior must change, return to
   `In Progress`, update implementation, and repeat review.
6. Run final tests and required checks against the integrated result.
7. Verify the integrated tree is equivalent to the approved implementation.
8. Push the target branch with a normal non-force push.
9. Verify the remote target branch contains the expected commit/tree and acceptance result.
10. Reconcile every affected PR: identify PRs whose approved implementation is now present,
    leave the honest reconciliation comment, and close every such PR that is still open.

The reconciliation comment SHOULD follow this form, with real values substituted:

```text
Integrated locally into `<target-branch>` after approval.

Verified in `<commit>`.

Closing this PR because its approved implementation is already present in the target branch.
```

The agent MUST NOT claim that GitHub merged a PR when local integration performed the merge. A PR
closed for this reason is processed, not evidence by itself that the Issue is `Done`.

## Unknown local changes

At session start and before integration, inspect `git status`, staged/unstaged diffs, and untracked
files. If the source or ownership of local changes is unknown, the agent MUST NOT reset, clean,
restore, overwrite, or destructively check out them. The agent MAY perform read-only inspection and
must stop any action that could discard or overwrite those changes until their source is safely
identified.

## Unauthorized target content

If approved implementation appears in the target branch before approval, or a Project/PR record
claims completion without matching remote evidence, treat it as a reconciliation incident. Do not
set `Done` merely because code exists. Determine whether the content was authorized, how it was
integrated, which acceptance criteria it satisfies, and whether the PR history is accurate; if
approval cannot be established, stop and report the missing authorization.
