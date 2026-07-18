---
name: checkout-hash
description: |
  Check out a local branch's current commit as detached HEAD in the main working directory.
  Use when development happens in a Git worktree but the project must run from the main directory
  that holds local environment configuration. Execute only on an explicit checkout request.
license: MIT
metadata:
  author: aa89227
  version: "2.0"
---

# Checkout By Hash

Load the committed state of a local worktree branch into the main working directory without
checking out the branch itself.

Git worktrees share local branch refs. Resolving the branch to a commit and checking out that commit
avoids Git's restriction against checking out the same branch in two worktrees.

## Usage

```text
sh <skill-directory>/scripts/checkout-hash.sh <local-branch>
```

Run the bundled script from the working directory that should become detached.

## Workflow

1. Require an explicit user request to perform the checkout. Discussion, analysis, or a mention of
   the branch is not authorization.
2. Ask for the local branch name when it is missing.
3. Run the bundled script with exactly one branch argument.
4. Report the source branch and resulting detached commit.
5. Stop and relay the error if the script refuses the operation.

## Guarantees And Limits

- Resolve only `refs/heads/<branch>`; do not fetch or fall back to a remote-tracking branch.
- Include only content committed to the branch. Uncommitted worktree changes are not available.
- Preserve ignored local files such as `.env`.
- Refuse checkout when the target working directory contains staged, tracked, or non-ignored
  untracked changes.
- Never stash, clean, reset, or force away user work.
- Treat the detached main directory as a runtime or verification environment, not a development
  branch. Do not create commits there as part of this workflow.

Do not reproduce the checkout with ad hoc Git commands if the script fails.
