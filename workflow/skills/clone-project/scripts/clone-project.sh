#!/usr/bin/env sh
set -eu

CACHE_MARKER_VALUE='agent-clone-cache-v2'
LOCK_STALE_SECONDS=300
LOCK_WAIT_SECONDS=60

usage() {
  printf 'Usage: %s <repo-url> [branch-or-tag]\n' "$0" >&2
  printf '       %s --purge <days>\n' "$0" >&2
}

die() {
  printf 'ERROR: %s\n' "$1" >&2
  exit "${2:-1}"
}

now_epoch() {
  date +%s
}

now_iso() {
  date -u +'%Y-%m-%dT%H:%M:%SZ'
}

read_file() {
  if [ -f "$1" ]; then
    cat "$1"
  fi
}

write_atomic() {
  destination=$1
  value=$2
  temporary="${destination}.tmp.$$"
  printf '%s\n' "$value" > "$temporary"
  mv "$temporary" "$destination"
}

valid_id() {
  case "$1" in
    ''|*[!0-9a-f]*) return 1 ;;
    *) return 0 ;;
  esac
}

is_managed_entry() {
  [ -f "$1/.managed-clone-cache" ] &&
    [ "$(read_file "$1/.managed-clone-cache")" = "$CACHE_MARKER_VALUE" ]
}

lock_path=''
temporary_entry=''

release_lock() {
  if [ -n "$lock_path" ] && [ -d "$lock_path" ]; then
    rm -f "$lock_path/pid" "$lock_path/created-at"
    rmdir "$lock_path" 2>/dev/null || true
  fi
  lock_path=''
}

cleanup() {
  if [ -n "$temporary_entry" ] && [ -d "$temporary_entry" ]; then
    rm -rf "$temporary_entry"
  fi
  release_lock
}

trap cleanup EXIT
trap 'cleanup; exit 130' HUP INT TERM

remove_stale_lock() {
  candidate=$1
  created=$(read_file "$candidate/created-at")
  owner_pid=$(read_file "$candidate/pid")

  case "$created" in
    ''|*[!0-9]*) return 1 ;;
  esac
  case "$owner_pid" in
    ''|*[!0-9]*) return 1 ;;
  esac

  if kill -0 "$owner_pid" 2>/dev/null; then
    return 1
  fi

  current=$(now_epoch)
  age=$((current - created))
  [ "$age" -ge "$LOCK_STALE_SECONDS" ] || return 1

  rm -f "$candidate/pid" "$candidate/created-at"
  rmdir "$candidate" 2>/dev/null
}

try_acquire_lock() {
  id=$1
  candidate="$LOCKS/$id"

  if mkdir "$candidate" 2>/dev/null; then
    lock_path=$candidate
  elif remove_stale_lock "$candidate" && mkdir "$candidate" 2>/dev/null; then
    lock_path=$candidate
  else
    return 1
  fi

  write_atomic "$lock_path/pid" "$$"
  write_atomic "$lock_path/created-at" "$(now_epoch)"
}

acquire_lock() {
  id=$1
  waited=0

  while ! try_acquire_lock "$id"; do
    if [ "$waited" -ge "$LOCK_WAIT_SECONDS" ]; then
      die "timed out waiting for cache lock: $id"
    fi
    sleep 1
    waited=$((waited + 1))
  done
}

repository_id() {
  printf '%s' "$1" | sha256sum | awk '{ print $1 }'
}

resolve_requested_ref() {
  repository=$1
  requested=$2

  if [ -n "$requested" ]; then
    if git -C "$repository" show-ref --verify --quiet "refs/remotes/origin/$requested"; then
      resolved_ref="refs/remotes/origin/$requested"
    elif git -C "$repository" show-ref --verify --quiet "refs/tags/$requested"; then
      resolved_ref="refs/tags/$requested"
    else
      return 1
    fi
  else
    resolved_ref=$(git -C "$repository" symbolic-ref --quiet refs/remotes/origin/HEAD 2>/dev/null) ||
      return 1
  fi

  resolved_commit=$(git -C "$repository" rev-parse --verify "${resolved_ref}^{commit}") ||
    return 1
}

prepare_worktree() {
  repository=$1
  commit=$2

  git -C "$repository" checkout --quiet --force --detach "$commit"
  git -C "$repository" reset --quiet --hard "$commit"
  git -C "$repository" clean --quiet -ffdx
}

purge_cache() {
  days=$1

  case "$days" in
    ''|*[!0-9]*) die "--purge requires a positive number of days" ;;
  esac
  [ "$days" -gt 0 ] || die "--purge requires a positive number of days"

  current=$(now_epoch)
  cutoff=$((current - days * 86400))
  removed=0
  skipped=0

  for entry in "$WORKSPACE"/*; do
    [ -d "$entry" ] || continue
    id=${entry##*/}
    valid_id "$id" || continue
    is_managed_entry "$entry" || continue

    last_used=$(read_file "$entry/last-used")
    case "$last_used" in
      ''|*[!0-9]*)
        printf 'WARNING: preserving cache with invalid last-used metadata: %s\n' "$entry" >&2
        skipped=$((skipped + 1))
        continue
        ;;
    esac

    [ "$last_used" -lt "$cutoff" ] || continue

    if ! try_acquire_lock "$id"; then
      printf 'WARNING: skipping active cache entry: %s\n' "$entry" >&2
      skipped=$((skipped + 1))
      continue
    fi

    if is_managed_entry "$entry"; then
      rm -rf "$entry"
      printf 'PURGED: %s\n' "$id"
      removed=$((removed + 1))
    fi
    release_lock
  done

  printf '%s\n' '---'
  printf 'PURGED: %s\n' "$removed"
  printf 'SKIPPED: %s\n' "$skipped"
}

cache_parent=${XDG_CACHE_HOME:-"$HOME/.cache"}
WORKSPACE=${CLONE_WORKSPACE:-"$cache_parent/agent-clone-projects"}
mkdir -p "$WORKSPACE"
WORKSPACE=$(cd "$WORKSPACE" && pwd -P)
[ "$WORKSPACE" != "/" ] || die "cache workspace must not be the filesystem root"

LOCKS="$WORKSPACE/.locks"
mkdir -p "$LOCKS"

if [ "${1:-}" = "--purge" ]; then
  [ "$#" -eq 2 ] || { usage; exit 64; }
  purge_cache "$2"
  exit 0
fi

[ "$#" -ge 1 ] && [ "$#" -le 2 ] || { usage; exit 64; }

repository_url=$1
requested_ref=${2:-}
id=$(repository_id "$repository_url")
valid_id "$id" || die "could not create a safe repository cache key"

entry="$WORKSPACE/$id"
repository="$entry/repo"
acquire_lock "$id"

cache_result='HIT'
cache_status='FRESH'

if [ -e "$entry" ]; then
  is_managed_entry "$entry" || die "cache path exists but is not managed by this script: $entry"
  [ "$(read_file "$entry/repository-url")" = "$repository_url" ] ||
    die "cache key collision or repository URL mismatch"
  git -C "$repository" rev-parse --is-inside-work-tree >/dev/null 2>&1 ||
    die "cached repository is invalid: $repository"
  expected_origin=$(read_file "$entry/origin-url")
  [ -n "$expected_origin" ] || die "cached repository has no managed origin metadata"
  [ "$(git -C "$repository" remote get-url origin)" = "$expected_origin" ] ||
    die "cached repository origin does not match its managed URL"

  printf 'CACHE: %s\n' "$repository"
  if git -C "$repository" fetch origin --prune --prune-tags --tags; then
    write_atomic "$entry/last-successful-fetch" "$(now_epoch)"
    write_atomic "$entry/last-successful-fetch-iso" "$(now_iso)"
  else
    cache_status='STALE'
    printf 'WARNING: refresh failed; using the last successfully fetched content if available.\n' >&2
  fi
else
  cache_result='MISS'
  temporary_entry=$(mktemp -d "$WORKSPACE/.clone-$id.XXXXXX")
  temporary_repository="$temporary_entry/repo"

  printf 'CLONE: %s\n' "$repository_url"
  if ! git clone -- "$repository_url" "$temporary_repository"; then
    die "initial clone failed and no cached content is available"
  fi

  printf '%s\n' "$CACHE_MARKER_VALUE" > "$temporary_entry/.managed-clone-cache"
  printf '%s\n' "$repository_url" > "$temporary_entry/repository-url"
  git -C "$temporary_repository" remote get-url origin > "$temporary_entry/origin-url"
  printf '%s\n' "$(now_epoch)" > "$temporary_entry/last-successful-fetch"
  printf '%s\n' "$(now_iso)" > "$temporary_entry/last-successful-fetch-iso"
  mv "$temporary_entry" "$entry"
  temporary_entry=''
fi

if ! resolve_requested_ref "$repository" "$requested_ref"; then
  if [ -n "$requested_ref" ]; then
    die "branch or tag '$requested_ref' is not available in the cache"
  fi
  die "the cached repository has no resolvable origin default branch"
fi

prepare_worktree "$repository" "$resolved_commit"
write_atomic "$entry/last-used" "$(now_epoch)"
last_successful_fetch=$(read_file "$entry/last-successful-fetch-iso")

printf '%s\n' '---'
printf 'PATH: %s\n' "$repository"
printf 'REF: %s\n' "$resolved_ref"
printf 'COMMIT: %s\n' "$resolved_commit"
printf 'CACHE_RESULT: %s\n' "$cache_result"
printf 'CACHE_STATUS: %s\n' "$cache_status"
printf 'LAST_SUCCESSFUL_FETCH: %s\n' "$last_successful_fetch"
