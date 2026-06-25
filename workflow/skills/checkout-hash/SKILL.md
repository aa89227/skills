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
  version: "1.0"
  tags: ["workflow", "git", "checkout", "detached-head"]
  trigger_keywords: ["checkout hash", "detached HEAD", "checkout by hash"]
---

# Checkout by Hash

Checkout a branch's latest commit as a **detached HEAD** instead of switching to the branch itself.

## Usage

```
/checkout-hash <branch-name>
```

## Workflow

1. **Resolve** — Run `git rev-parse <branch-name>` to get the full commit hash.
   - If the branch name does not exist locally, try `origin/<branch-name>`.
   - If neither resolves, report the error and stop.
2. **Report** — Show the resolved hash and branch name to the user.
3. **Checkout** — Run `git checkout <full-hash>`.
4. **Confirm** — Run `git log --oneline -1` to confirm the current HEAD.

## Rules

- **Never run `git checkout <branch-name>`** — always resolve to a hash first, then checkout the hash.
- If the working tree has uncommitted changes that would conflict, warn the user and stop — do not use `--force` or stash automatically.
- If no branch name is provided in the arguments, ask the user which branch to checkout.
