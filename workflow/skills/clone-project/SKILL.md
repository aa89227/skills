---
name: clone-project
description: |
  Clone an external git repository locally for read-only investigation. Use ONLY when the user
  provides a git URL or explicitly references an external/other repository they want to examine,
  or when an agent needs to clone an external repo to understand its implementation.
  Do NOT trigger for requests about the current working directory's repo.
  Trigger phrases: "clone this repo <url>", "clone down <url>", "look at <url>",
  "clone external project", "clone another repo", "pull down this repo for reference".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["workflow", "git", "clone", "repository", "investigation"]
  trigger_keywords: ["clone repo", "clone project", "clone external",
    "clone another repo", "pull down repo", "clone for reference"]
---

# Clone Project

Clone or update a git repository for read-only investigation.

## When to Use

- You need to **read, grep, or explore** another project's codebase — not just its metadata.
- `gh api` / `gh pr view` only gives partial info; you need the full source tree.
- An agent or workflow needs a local copy of a repo as reference material.

## Usage

```bash
# Clone or update a repo
sh <this-skill-directory>/scripts/clone-project.sh <repo-url> [branch]

# Purge repos unused for N days
sh <this-skill-directory>/scripts/clone-project.sh --purge <days>
```

### Arguments

| Arg | Required | Description |
|-----|----------|-------------|
| `repo-url` | Yes | Any valid git URL (`https://...`, `git@...`) |
| `branch` | No | Branch or tag to checkout. Omit for default branch |

### Examples

```bash
# Clone with default branch
sh workflow/skills/clone-project/scripts/clone-project.sh https://github.com/ehanlin/item-bank

# Clone specific branch
sh workflow/skills/clone-project/scripts/clone-project.sh https://github.com/ehanlin/item-bank feature/new-api

# Purge repos not used in 30 days
sh workflow/skills/clone-project/scripts/clone-project.sh --purge 30
```

## Behavior

| Scenario | Action |
|----------|--------|
| Repo not yet cloned | `git clone` to a new directory |
| Repo exists in index | `git fetch --all` then `reset --hard` to latest |
| Different branch requested | Force checkout + reset to origin |
| Branch not found | Reports error |
| `--purge <days>` | Removes repos with `last_used` older than N days |

## Output

Script 最後會印出 `PATH`，**直接用這個路徑**去讀取、grep、探索 cloned project。

## Rules

1. **Always use this script** to obtain a local copy — do not run raw `git clone`.
2. **Workspace is read-only** — never modify, commit, or push in the cloned workspace.
3. After cloning, use **Read, Grep, Bash (read-only)** on the script output `PATH` to investigate.
4. If the script fails (auth, network, missing repo), report the error — do not retry with different credentials.
