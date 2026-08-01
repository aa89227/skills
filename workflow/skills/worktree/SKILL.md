---
name: worktree
description: |
  Create or remove a project-local Git worktree for a ticket, issue, or task under
  `.worktrees/<ticket>` inside the repository, with `.worktrees/` gitignored and a
  complete, user-language `worktree-task.md` handoff briefing in the new worktree. "Ticket"
  covers any tracker identifier — Shortcut (sc-100), GitHub issue (#123, gh-123), Jira
  (PROJ-123), or a free-form task slug. Use when the user asks to open, create, or clean up
  a worktree for a ticket/issue, or says "worktree", "開 worktree", "開分支處理這張票".
license: MIT
metadata:
  author: aa89227
  version: "1.1"
---

# Worktree

Create or remove a Git worktree scoped to a single ticket, keeping it inside the project
(`.worktrees/<ticket>`) instead of an external directory, so it travels with the repo and
stays out of version control.

`worktree-task.md` is a handoff snapshot for a new agent session. It is not a generic marker
containing only branch metadata. The bundled script creates an incomplete scaffold because the
shell process does not know the user's request; the agent that creates the worktree must fill in
the actual briefing before handing the worktree to the user.

## Usage

```text
sh <skill-directory>/scripts/worktree.sh add <ticket> [base] [prefix]
sh <skill-directory>/scripts/worktree.sh validate <ticket>
sh <skill-directory>/scripts/worktree.sh remove <ticket>
```

Run the script from the project's main working directory (not from inside another worktree).

- `ticket` — any tracker identifier: `sc-100`, `#123`, `gh-123`, `PROJ-123`, or a plain slug.
  Surrounding `#`, `[`, `]`, and whitespace are stripped automatically.
- `base` — branch to create from when the ticket branch doesn't exist yet. Defaults to the
  repo's detected default branch (`origin/HEAD`, falling back to `main` or `master`).
- `prefix` — branch prefix. Defaults to `feat`; pass `fix`, `chore`, `hotfix`, etc. as needed.

`validate` performs structural checks on the completed briefing. It cannot determine whether the
requirements are correct or whether the prose uses the user's language; those are the creating
agent's responsibilities.

## Workflow

1. Parse the ticket from the user's request; ask if it is ambiguous or missing. Retain the full
   request and the user's interaction language for the handoff; do not reduce the request to the
   ticket name.
2. If the user didn't state a base branch and the default detected by the script would be
   surprising (e.g. multiple release branches in play), confirm the base with the user or list
   `git branch -a` before running `add`. Otherwise let the script auto-detect it.
3. Pick `prefix` from the nature of the work (`feat`, `fix`, `chore`, `hotfix`, ...); default
   to `feat` when nothing indicates otherwise.
4. Run `add` via the bundled script. Do not reimplement its Git worktree steps with ad hoc
   commands.
5. Immediately complete `worktree-task.md` in the new worktree before reporting success. Replace
   the scaffold with a self-contained briefing based on the complete current conversation and
   repository evidence. Keep the generated worktree metadata, the machine-readable section
   markers, and the cleanup note.
6. Write explanatory prose in the user's interaction language. Preserve the original request
   verbatim in its own section. Keep exact file paths, identifiers, commands, API names, and
   other code symbols unchanged. If the request mixes languages, use the dominant interaction
   language and preserve quoted source text exactly.
7. Separate confirmed requirements from inferred assumptions and unresolved questions. Never
   turn an agent inference into a confirmed requirement. If a section has no entries, explicitly
   write the equivalent of “none recorded” in the user's language; do not leave placeholders.
8. Run `validate <ticket>`. Do not hand off an incomplete or unvalidated briefing. If a material
   requirement is unresolved, record it as an unresolved question and ask the user when it blocks
   safe implementation.
9. On `add`, report the worktree path, branch, base, and completed task-file path from the script's
   output. Then tell the user to open a new session in that folder and paste `@worktree-task.md`
   or "讀取此資料夾底下的 worktree-task.md" to load the task briefing.
10. On `remove`, confirm the reported path matches what the user intended before it is gone.

## Briefing contract

The completed file must be understandable without the creating session's hidden context. Include
these sections, using headings and prose in the user's language:

- Worktree metadata: ticket, branch, base, project, briefing language, and requirement status.
- User's original request: the complete request, quoted or otherwise clearly distinguished from
  agent-authored content.
- Context and goal: the problem, why it matters, desired outcome, and affected behavior.
- Confirmed requirements: concrete behavior that must be implemented.
- Scope: what is included and explicitly not included.
- Expected behavior and examples: scenarios, inputs, outputs, or Given/When/Then cases when they
  make the requirement clearer.
- Constraints and exceptions: repository rules, architecture, compatibility, security, or
  explicitly approved deviations.
- Inferred assumptions: decisions made from repository evidence or ordinary defaults.
- Unresolved questions: ambiguity, missing authority, or decisions that may change the result.
- Acceptance criteria: separate automated checks from manual verification, with observable
  expected results.
- References and initial investigation: relevant instructions, files, symbols, tests, and
  commands. Label guesses or unverified paths as preliminary.
- Handoff notes: what to read or run first, what must not be expanded, and the reminder to remove
  `worktree-task.md` before committing.

The briefing should contain inspectable facts and decisions only; do not include hidden
chain-of-thought. A later explicit user instruction supersedes this snapshot and should be
recorded as a requirement revision when it changes the task.

## What `add` does

- Ensures `.worktrees/` is listed in the project's `.gitignore` (adds it if missing). This edit is
  left uncommitted — mention it to the user rather than committing it yourself.
- Creates branch `<prefix>/<ticket>` from `base` if it doesn't already exist; reuses it otherwise.
- Adds the worktree at `<project-root>/.worktrees/<ticket>`.
- Writes an incomplete, structured `worktree-task.md` scaffold with the ticket, branch, base, and
  project metadata. The creating agent must replace its placeholders with the actual briefing and
  set its status to complete before handoff.
- If the worktree already exists, does nothing and reports `EXISTS` — it never overwrites or
  recreates one.

## What `validate` does

- Checks that the exact target worktree and `worktree-task.md` exist.
- Checks that the briefing is marked complete, still identifies the target ticket/branch/base/
  project, contains every required section marker, and contains no scaffold placeholder marker.
- Does not overwrite the briefing or assess the truth, completeness, or language of its prose.

## What `remove` does

- Deletes only the self-generated `worktree-task.md` inside the target worktree first — this file
  is otherwise untracked (the branch it was created from predates the `.gitignore` update) and
  would make Git refuse to remove the worktree at all.
- Then runs `git worktree remove` **without** `--force`, so any other uncommitted or untracked
  work in that worktree still blocks removal and is never silently discarded.

## Guarantees And Limits

- Never runs outside a Git repository; resolves the root via `git rev-parse --show-toplevel`.
- Never force-removes a worktree with genuine uncommitted work — only the known
  `worktree-task.md` file is cleared before removal, nothing else.
- Never deletes or force-overwrites an existing worktree, branch, or `.gitignore` content —
  `remove` only removes the exact `.worktrees/<ticket>` path via `git worktree remove`.
- Does not fast-forward, rebase, or merge an existing branch when reusing it.
- Do not fall back to manual `git worktree` commands if the script refuses — relay the error.
