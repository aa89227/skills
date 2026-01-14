---
name: git-commit-messages
description: Write high-signal Git commit messages. Use when preparing a commit, reviewing commit history, or defining commit conventions.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["git", "commit", "conventional-commits", "workflow"]
---

# Git Commit Messages

## Goals

- Make history readable: explain intent (why), not a file-by-file diff (what).
- Keep commits small and scoped.
- Avoid committing secrets (`.env`, tokens, private keys).

## Format

Prefer a Conventional Commits-style header:

- `type(scope): summary`

Optional:
- Body (1-3 lines): motivation, constraints, trade-offs
- Footer: `BREAKING CHANGE: ...` and/or issue links

## Types

- `feat`: new user-facing behavior
- `fix`: bug fix
- `refactor`: behavior-preserving restructure
- `perf`: performance improvement
- `test`: add/change tests
- `docs`: documentation-only changes
- `chore`: tooling, CI, deps, build scripts

## Rules of Thumb

- Use imperative mood: "add", "fix", "remove", "prevent".
- Keep summary <= ~72 chars.
- One intent per commit.
- If risky, state mitigations in the body.

## Examples

- `feat(auth): support passkeys for login`
- `fix(api): handle null customer id in invoice endpoint`
- `refactor(storage): simplify S3 retry policy`
- `chore(ci): pin dotnet sdk to 10.0.1xx`

Body example:

```
fix(sync): avoid duplicate uploads on retry

The retry loop re-enqueued items without deduping. Track in-flight ids
and skip duplicates to keep behavior idempotent.
```

## Pre-commit Checklist

- `git status` is clean except intended changes.
- `git diff` matches the commit intent.
- No secrets included.
