---
name: git-worktrees
description: Use git worktree for parallel branch workflows without constant switching. Use when working on multiple tasks/PRs at once.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["git", "worktree", "workflow"]
---

# Git Worktrees

## What it is

`git worktree` lets you attach multiple working directories to the same repository.
It keeps builds isolated and avoids frequent branch switching.

## Common Commands

- List worktrees:
  - `git worktree list`

- Add a worktree for an existing branch:
  - `git worktree add ../wt-feature feature/my-branch`

- Add a worktree and create a new branch from a base branch:
  - `git worktree add -b feature/new-thing ../wt-new origin/main`

- Remove a worktree:
  - `git worktree remove ../wt-feature`

- Clean up stale metadata:
  - `git worktree prune`

## Recommended Workflow

1. Keep your main directory on `main` (or `master`).
2. Create one worktree per task/PR.
3. Run tests/builds inside that worktree.
4. When merged, delete the branch and remove the worktree.

## Pitfalls

- A branch checked out in a worktree cannot be checked out elsewhere.
- If you delete a worktree folder manually, run `git worktree prune`.
- Tools that assume a single working directory may need per-worktree config.
