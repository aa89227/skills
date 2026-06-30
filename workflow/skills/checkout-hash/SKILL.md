---
name: checkout-hash
description: |
  Checkout a branch's latest commit by hash (detached HEAD) instead of checking out the branch
  directly. Resolves the branch to its HEAD commit hash, then runs git checkout on that hash.
  Trigger phrases: "checkout hash", "detached HEAD", "checkout by hash",
  "checkout 最新 hash", "checkout 分支 hash".
license: MIT
metadata:
  author: aa89227
  version: "1.1"
  tags: ["workflow", "git", "checkout", "detached-head"]
  trigger_keywords: ["checkout hash", "detached HEAD", "checkout by hash"]
---

# Checkout by Hash

Checkout a branch's latest commit as a **detached HEAD** instead of switching to the branch itself.

## Usage

```
sh <this-skill-directory>/scripts/checkout-hash.sh <branch-name>
```

## Workflow

Run the bundled script directly:

```bash
sh workflow/skills/checkout-hash/scripts/checkout-hash.sh <branch-name>
```

If the skill is installed somewhere else, use the `scripts/checkout-hash.sh` path next to this `SKILL.md`.

The script handles:

1. Resolving the branch to a full commit hash.
2. Trying the local branch first, then `origin/<branch-name>`.
3. Refusing to proceed when the working tree has uncommitted changes.
4. Checking out the resolved hash as detached HEAD.
5. Confirming the current HEAD hash.

## Rules

- Do not manually inspect git history, diffs, or status output for the normal workflow. The script is the workflow.
- Do not manually run `git rev-parse`, `git log`, or `git checkout` unless maintaining or debugging the bundled script.
- **Never run `git checkout <branch-name>`**. The only checkout path is the script, which checks out a commit hash.
- If the script reports uncommitted changes, stop and relay that message. Do not use `--force` or stash automatically.
- If no branch name is provided in the arguments, ask the user which branch to checkout.
