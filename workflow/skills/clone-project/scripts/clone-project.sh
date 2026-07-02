#!/bin/bash
set -euo pipefail

# Usage:
#   clone-project.sh <repo-url> [branch]           Clone or update a repo
#   clone-project.sh --purge <days>                 Remove repos unused for <days> days
#
# Workspace: ~/.claude/workspaces (override via CLONE_WORKSPACE env var)
# Index:     <workspace>/.index (tsv: uuid \t repo-url \t last_used)

WORKSPACE="${CLONE_WORKSPACE:-/tmp/agent-clone-projects}"
INDEX="$WORKSPACE/.index"

mkdir -p "$WORKSPACE"
touch "$INDEX"

now_iso() {
  date -u +"%Y-%m-%dT%H:%M:%SZ"
}

update_index() {
  local uuid="$1" repo="$2" ts
  ts=$(now_iso)
  if grep -q "^${uuid}	" "$INDEX" 2>/dev/null; then
    sed -i'' -e "s|^${uuid}	.*|${uuid}	${repo}	${ts}|" "$INDEX"
  else
    printf '%s\t%s\t%s\n' "$uuid" "$repo" "$ts" >> "$INDEX"
  fi
}

find_by_repo() {
  grep "	${1}	" "$INDEX" 2>/dev/null | head -1 | cut -f1
}

# --purge <days>
if [ "${1:-}" = "--purge" ]; then
  DAYS="${2:-}"
  if [ -z "$DAYS" ] || ! [ "$DAYS" -gt 0 ] 2>/dev/null; then
    echo "ERROR: --purge requires a positive number of days" >&2
    exit 1
  fi

  CUTOFF=$(date -u -v-"${DAYS}"d +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null \
    || date -u -d "${DAYS} days ago" +"%Y-%m-%dT%H:%M:%SZ" 2>/dev/null)

  if [ -z "$CUTOFF" ]; then
    echo "ERROR: cannot compute cutoff date" >&2
    exit 1
  fi

  REMOVED=0
  while IFS=$'\t' read -r uuid repo last_used; do
    [ -z "$uuid" ] && continue
    if [[ "$last_used" < "$CUTOFF" ]]; then
      TARGET="$WORKSPACE/$uuid"
      if [ -d "$TARGET" ]; then
        rm -rf "$TARGET"
        echo "PURGED: $repo ($uuid) — last used $last_used"
      fi
      sed -i'' -e "/^${uuid}	/d" "$INDEX"
      REMOVED=$((REMOVED + 1))
    fi
  done < "$INDEX"

  echo "---"
  echo "PURGED: $REMOVED repo(s) older than $DAYS days"
  exit 0
fi

# Clone / update
REPO="${1:-}"
BRANCH="${2:-}"

if [ -z "$REPO" ]; then
  echo "ERROR: repository URL is required" >&2
  echo "Usage: clone-project.sh <repo-url> [branch]" >&2
  echo "       clone-project.sh --purge <days>" >&2
  exit 1
fi

UUID=$(find_by_repo "$REPO")

if [ -n "$UUID" ] && [ -d "$WORKSPACE/$UUID/.git" ]; then
  TARGET="$WORKSPACE/$UUID"
  echo "EXISTING: $TARGET"
  cd "$TARGET"

  git fetch --all --prune 2>&1

  if [ -n "$BRANCH" ]; then
    git checkout -f "$BRANCH" 2>/dev/null \
      || git checkout -f -b "$BRANCH" "origin/$BRANCH" 2>/dev/null \
      || { echo "ERROR: branch '$BRANCH' not found" >&2; exit 1; }
    git reset --hard "origin/$BRANCH" 2>/dev/null || true
  else
    DEFAULT_BRANCH=$(git symbolic-ref refs/remotes/origin/HEAD 2>/dev/null | sed 's@^refs/remotes/origin/@@')
    if [ -n "$DEFAULT_BRANCH" ]; then
      git checkout -f "$DEFAULT_BRANCH" 2>&1
      git reset --hard "origin/$DEFAULT_BRANCH" 2>&1
    fi
  fi

  update_index "$UUID" "$REPO"
else
  UUID=$(uuidgen | tr '[:upper:]' '[:lower:]')
  TARGET="$WORKSPACE/$UUID"

  echo "CLONE: $REPO -> $TARGET"

  if [ -n "$BRANCH" ]; then
    git clone --branch "$BRANCH" "$REPO" "$TARGET" 2>&1
  else
    git clone "$REPO" "$TARGET" 2>&1
  fi

  update_index "$UUID" "$REPO"
fi

cd "$TARGET"
echo "---"
echo "PATH: $TARGET"
echo "BRANCH: $(git branch --show-current 2>/dev/null || echo 'detached')"
echo "COMMIT: $(git rev-parse --short HEAD 2>/dev/null)"
echo "UPDATED: $(git log -1 --format='%ci' 2>/dev/null)"
