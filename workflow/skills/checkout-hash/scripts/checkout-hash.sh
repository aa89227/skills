#!/usr/bin/env sh
set -eu

usage() {
  printf 'Usage: %s <branch-name>\n' "$0" >&2
}

die() {
  printf 'Error: %s\n' "$1" >&2
  exit "${2:-1}"
}

resolve_commit() {
  branch=$1

  for ref in "refs/heads/$branch" "refs/remotes/$branch" "refs/remotes/origin/$branch"; do
    if commit=$(git rev-parse --verify --quiet "${ref}^{commit}"); then
      printf '%s %s\n' "$commit" "$ref"
      return 0
    fi
  done

  if expr "$branch" : 'refs/' >/dev/null; then
    if commit=$(git rev-parse --verify --quiet "${branch}^{commit}"); then
      printf '%s %s\n' "$commit" "$branch"
      return 0
    fi
  fi

  return 1
}

has_uncommitted_changes() {
  ! git diff --quiet --ignore-submodules -- ||
    ! git diff --cached --quiet --ignore-submodules -- ||
    test -n "$(git ls-files --others --exclude-standard)"
}

if test "$#" -ne 1; then
  usage
  exit 64
fi

branch=$1

git rev-parse --is-inside-work-tree >/dev/null 2>&1 ||
  die "this command must be run inside a git worktree." 128

if resolved=$(resolve_commit "$branch"); then
  commit=${resolved%% *}
  ref=${resolved#* }
else
  die "could not resolve '$branch' as a local branch or origin branch." 1
fi

printf 'Resolved %s to %s via %s\n' "$branch" "$commit" "$ref"

if has_uncommitted_changes; then
  die "working tree has uncommitted changes; commit, stash, or discard them before checkout." 2
fi

if ! git checkout --quiet --detach "$commit"; then
  die "git checkout failed. No force checkout or stash was attempted." 1
fi

current=$(git rev-parse --verify HEAD)

if test "$current" != "$commit"; then
  die "checkout verification failed; HEAD is $current, expected $commit." 1
fi

printf 'Detached HEAD is now at %s\n' "$current"
