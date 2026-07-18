---
name: clone-project
description: |
  Clone or refresh an external Git repository in a persistent local cache for read-only source
  investigation. Reuse cached content across sessions and, when refresh fails, continue from the
  last successful fetch with an explicit stale-data warning. Do not use for the current repository.
license: MIT
metadata:
  author: aa89227
  version: "2.0"
---

# Clone Project

Obtain a reusable local copy of an external repository when full source inspection is needed.

## Requirements

- A POSIX-compatible `sh` environment such as Git Bash.
- Git and the standard utilities bundled with Git Bash.
- Network access for the initial clone and best-effort refresh.

## Usage

```text
sh <skill-directory>/scripts/clone-project.sh <repo-url> [branch-or-tag]
sh <skill-directory>/scripts/clone-project.sh --purge <days>
```

Use the `PATH` printed by the script for subsequent read-only investigation.

## Cache Behavior

- Cache repositories across sessions under a persistent user cache directory.
- Reuse the same cache entry for an exact repository URL.
- Attempt to fetch before every reuse.
- Use the refreshed content when fetch succeeds.
- Continue with the last successfully fetched content when fetch fails and the requested ref is
  already available in cache.
- Fail when no cache exists or the requested ref is unavailable.
- Reset and clean the managed cached worktree before returning it. Content written inside this
  disposable cache is not preserved.
- Serialize updates to the same repository with a per-repository lock.

Set `CLONE_WORKSPACE` to override the default cache root.

## Freshness Reporting

Read these structured output fields:

- `PATH`: Cached repository path.
- `REF`: Resolved remote branch or tag.
- `COMMIT`: Checked-out commit.
- `CACHE_RESULT`: `HIT` or `MISS`.
- `CACHE_STATUS`: `FRESH` or `STALE`.
- `LAST_SUCCESSFUL_FETCH`: UTC timestamp of the last successful clone or fetch.

When `CACHE_STATUS` is `STALE`, tell the user that:

- the refresh failed;
- investigation used cached content;
- the cache's last successful fetch time; and
- remote changes after that time may be absent.

Do not silently substitute another branch or tag.

## Safety

1. Use this script only for an explicitly external repository or when full external source is
   necessary for the user's request.
2. Treat the returned workspace as read-only. Do not modify, commit, or push from it.
3. Never place user work inside the managed cache.
4. Run purge only on an explicit purge request.
5. If authentication fails, use eligible stale cache or report the failure. Do not try alternate
   credentials.
