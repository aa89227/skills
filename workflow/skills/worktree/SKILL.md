---
name: worktree
description: |
  Create or remove a project-local Git worktree for a ticket, issue, or task under
  `.worktrees/<ticket>` inside the repository, with `.worktrees/` gitignored and a
  `worktree-task.md` briefing file generated in the new worktree. "Ticket" covers any
  tracker identifier — Shortcut (sc-100), GitHub issue (#123, gh-123), Jira (PROJ-123),
  or a free-form task slug. Use when the user asks to open, create, or clean up a
  worktree for a ticket/issue, or says "worktree", "開 worktree", "開分支處理這張票".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
---

# Worktree

Create or remove a Git worktree scoped to a single ticket, keeping it inside the project
(`.worktrees/<ticket>`) instead of an external directory, so it travels with the repo and
stays out of version control.

## Usage

```text
sh <skill-directory>/scripts/worktree.sh add <ticket> [base] [prefix]
sh <skill-directory>/scripts/worktree.sh remove <ticket>
```

Run the script from the project's main working directory (not from inside another worktree).

- `ticket` — any tracker identifier: `sc-100`, `#123`, `gh-123`, `PROJ-123`, or a plain slug.
  Surrounding `#`, `[`, `]`, and whitespace are stripped automatically.
- `base` — branch to create from when the ticket branch doesn't exist yet. Defaults to the
  repo's detected default branch (`origin/HEAD`, falling back to `main` or `master`).
- `prefix` — branch prefix. Defaults to `feat`; pass `fix`, `chore`, `hotfix`, etc. as needed.

## Workflow

1. Parse the ticket from the user's request; ask if it's ambiguous or missing.
2. If the user didn't state a base branch and the default detected by the script would be
   surprising (e.g. multiple release branches in play), confirm the base with the user or
   list `git branch -a` before running `add`. Otherwise let the script auto-detect it.
3. Pick `prefix` from the nature of the work (`feat`, `fix`, `chore`, `hotfix`, ...); default
   to `feat` when nothing indicates otherwise.
4. Run `add` or `remove` via the bundled script. Do not reimplement its steps with ad hoc
   `git worktree` commands.
5. On `add`, report the worktree path, branch, and base from the script's output, then tell
   the user to open a new session in that folder and paste `@worktree-task.md` or
   "讀取此資料夾底下的 worktree-task.md" to load the task briefing.
6. On `remove`, confirm the reported path matches what the user intended before it's gone.

## What `add` does

- Ensures `.worktrees/` is listed in the project's `.gitignore` (adds it if missing). This
  edit is left uncommitted — mention it to the user rather than committing it yourself.
- Creates branch `<prefix>/<ticket>` from `base` if it doesn't already exist; reuses it
  otherwise.
- Adds the worktree at `<project-root>/.worktrees/<ticket>`.
- Writes `worktree-task.md` at the worktree root with the ticket, branch, base, and project
  name, plus a reminder to delete that file before committing.
- If the worktree already exists, does nothing and reports `EXISTS` — it never overwrites or
  recreates one.

## What `remove` does

- Deletes only the self-generated `worktree-task.md` inside the target worktree first — this
  file is otherwise untracked (the branch it was created from predates the `.gitignore`
  update) and would make Git refuse to remove the worktree at all.
- Then runs `git worktree remove` **without** `--force`, so any other uncommitted or
  untracked work in that worktree still blocks removal and is never silently discarded.

## Guarantees And Limits

- Never runs outside a Git repository; resolves the root via `git rev-parse --show-toplevel`.
- Never force-removes a worktree with genuine uncommitted work — only the known
  `worktree-task.md` file is cleared before removal, nothing else.
- Never deletes or force-overwrites an existing worktree, branch, or `.gitignore` content —
  `remove` only removes the exact `.worktrees/<ticket>` path via `git worktree remove`.
- Does not fast-forward, rebase, or merge an existing branch when reusing it.
- Do not fall back to manual `git worktree` commands if the script refuses — relay the error.
