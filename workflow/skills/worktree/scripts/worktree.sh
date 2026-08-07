#!/usr/bin/env sh
set -eu

usage() {
  printf 'Usage: %s add <ticket> [base] [prefix]\n' "$0" >&2
  printf '       %s validate <ticket>\n' "$0" >&2
  printf '       %s remove <ticket>\n' "$0" >&2
}

die() {
  printf 'Error: %s\n' "$1" >&2
  exit "${2:-1}"
}

sanitize_ticket() {
  printf '%s' "$1" | tr -d '#[]' | tr -d '[:space:]'
}

project_root() {
  git rev-parse --show-toplevel 2>/dev/null || die "not inside a git repository" 128
}

ensure_gitignore_entry() {
  root=$1
  entry=$2
  gitignore="$root/.gitignore"

  if [ -f "$gitignore" ] && grep -qxF "$entry" "$gitignore"; then
    return 0
  fi

  printf '%s\n' "$entry" >> "$gitignore"
  printf 'Added %s to %s\n' "$entry" "$gitignore"
}

default_base_branch() {
  root=$1
  ref=$(git -C "$root" symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>/dev/null) || ref=''

  if [ -n "$ref" ]; then
    printf '%s' "${ref#origin/}"
    return 0
  fi

  for candidate in main master; do
    if git -C "$root" show-ref --verify --quiet "refs/heads/$candidate"; then
      printf '%s' "$candidate"
      return 0
    fi
  done

  die "could not determine the default base branch; pass one explicitly"
}

write_task_scaffold() {
  task_file=$1
  ticket=$2
  branch=$3
  base=$4
  project=$5

  cat > "$task_file" <<EOF
# Worktree Task

<!-- worktree-task-status: incomplete -->
<!-- worktree-task-ticket: $ticket -->
<!-- worktree-task-branch: $branch -->
<!-- worktree-task-base: $base -->
<!-- worktree-task-project: $project -->
<!-- worktree-task-implementation-intent: unspecified -->
<!-- worktree-task-implementation-status: not-started -->
<!-- worktree-task-next-action: unspecified -->
<!-- This scaffold must be completed by the agent that created the worktree before handoff. -->

## Worktree metadata

- Ticket: $ticket
- Branch: $branch
- Base: $base
- Project: $project
- Briefing language: replace this with the user's interaction language
- Briefing status: replace this with the briefing document status
- Requirements status: replace this with the current requirements status
- Implementation intent: replace this with requested, not-requested, or unclear
- Implementation status: not-started
- Next action: replace this with implement, await-user, or clarify

## User's original request
<!-- worktree-task-section: original-request -->
<!-- worktree-task-placeholder: preserve the complete original user request here -->
<!-- /worktree-task-section: original-request -->

## Context and goal
<!-- worktree-task-section: context-and-goal -->
<!-- worktree-task-placeholder: describe the problem, why it matters, and desired outcome -->
<!-- /worktree-task-section: context-and-goal -->

## Confirmed requirements
<!-- worktree-task-section: confirmed-requirements -->
<!-- worktree-task-placeholder: list concrete behavior confirmed by the user -->
<!-- /worktree-task-section: confirmed-requirements -->

## Scope
<!-- worktree-task-section: scope -->
<!-- worktree-task-placeholder: state what is included and explicitly excluded -->
<!-- /worktree-task-section: scope -->

## Expected behavior and examples
<!-- worktree-task-section: behavior -->
<!-- worktree-task-placeholder: add scenarios, examples, or Given/When/Then cases when useful -->
<!-- /worktree-task-section: behavior -->

## Constraints and exceptions
<!-- worktree-task-section: constraints -->
<!-- worktree-task-placeholder: record repository rules, technical constraints, and exceptions -->
<!-- /worktree-task-section: constraints -->

## Inferred assumptions
<!-- worktree-task-section: assumptions -->
<!-- worktree-task-placeholder: record inferences; do not present them as confirmed requirements -->
<!-- /worktree-task-section: assumptions -->

## Unresolved questions
<!-- worktree-task-section: open-questions -->
<!-- worktree-task-placeholder: record unresolved or blocking questions, or state that none exist -->
<!-- /worktree-task-section: open-questions -->

## Execution state
<!-- worktree-task-section: execution-state -->
<!-- worktree-task-placeholder: distinguish briefing completeness, implementation intent, implementation status, and next action; the file itself does not grant authority -->
<!-- /worktree-task-section: execution-state -->

## Acceptance criteria
<!-- worktree-task-section: acceptance -->
<!-- worktree-task-placeholder: separate automated checks from manual verification and expected results -->
<!-- /worktree-task-section: acceptance -->

## References and initial investigation
<!-- worktree-task-section: references -->
<!-- worktree-task-placeholder: list verified instructions, paths, symbols, tests, and commands -->
<!-- /worktree-task-section: references -->

## Handoff notes
<!-- worktree-task-section: handoff -->
<!-- worktree-task-placeholder: state what to read/run first and what must not be expanded -->
- Before committing, remove this generated file: worktree-task.md
<!-- /worktree-task-section: handoff -->
EOF
}

cmd_add() {
  ticket_raw=$1
  base_arg=${2:-}
  prefix=${3:-feat}

  ticket=$(sanitize_ticket "$ticket_raw")
  [ -n "$ticket" ] || die "ticket is required"

  root=$(project_root)
  worktree_dir="$root/.worktrees/$ticket"

  if [ -e "$worktree_dir" ]; then
    printf 'EXISTS: %s (not recreated)\n' "$worktree_dir"
    return 0
  fi

  base=$base_arg
  [ -n "$base" ] || base=$(default_base_branch "$root")

  branch="$prefix/$ticket"

  ensure_gitignore_entry "$root" ".worktrees/"

  if git -C "$root" show-ref --verify --quiet "refs/heads/$branch"; then
    git -C "$root" worktree add "$worktree_dir" "$branch"
  else
    git -C "$root" worktree add -b "$branch" "$worktree_dir" "$base"
  fi

  task_file="$worktree_dir/worktree-task.md"
  project=$(basename "$root")

  write_task_scaffold "$task_file" "$ticket" "$branch" "$base" "$project"

  printf '%s\n' '---'
  printf 'WORKTREE: %s\n' "$worktree_dir"
  printf 'BRANCH: %s\n' "$branch"
  printf 'BASE: %s\n' "$base"
  printf 'TASK_FILE: %s\n' "$task_file"
  printf 'TASK_FILE_STATUS: incomplete (populate it, then run validate)\n'
}

validate_section() {
  task_file=$1
  section=$2
  start="<!-- worktree-task-section: $section -->"
  end="<!-- /worktree-task-section: $section -->"

  section_content=$(awk -v start="$start" -v end="$end" '
    $0 == start { inside = 1; next }
    $0 == end { found = 1; inside = 0; next }
    inside { print }
    END { if (!found) exit 1 }
  ' "$task_file") || die "task briefing section is missing or malformed: $section"

  [ -n "$(printf '%s' "$section_content" | tr -d '[:space:]')" ] ||
    die "task briefing section is empty: $section"
}

cmd_validate() {
  ticket_raw=$1
  ticket=$(sanitize_ticket "$ticket_raw")
  [ -n "$ticket" ] || die "ticket is required"

  root=$(project_root)
  worktree_dir="$root/.worktrees/$ticket"
  task_file="$worktree_dir/worktree-task.md"
  project=$(basename "$root")

  [ -d "$worktree_dir" ] || die "worktree not found: $worktree_dir"
  [ -f "$task_file" ] || die "task briefing not found: $task_file"
  branch=$(git -C "$worktree_dir" symbolic-ref --quiet --short HEAD 2>/dev/null) ||
    die "could not determine the worktree branch: $worktree_dir"

  status=$(sed -n 's/^<!-- worktree-task-status: \([^ ]*\) -->$/\1/p' "$task_file" | head -n 1)
  [ "$status" = complete ] ||
    die "task briefing is incomplete: set worktree-task-status to complete after populating it"

  grep -qF "<!-- worktree-task-ticket: $ticket -->" "$task_file" ||
    die "task briefing ticket does not match: $ticket"
  grep -qF "<!-- worktree-task-branch: $branch -->" "$task_file" ||
    die "task briefing branch does not match: $branch"
  grep -qF "<!-- worktree-task-base:" "$task_file" ||
    die "task briefing base metadata is missing"
  grep -qF "<!-- worktree-task-project: $project -->" "$task_file" ||
    die "task briefing project does not match: $project"

  if grep -qF '<!-- worktree-task-placeholder:' "$task_file"; then
    die "task briefing still contains scaffold placeholders"
  fi
  if grep -qF 'This scaffold must be completed' "$task_file"; then
    die "task briefing still contains the scaffold handoff warning"
  fi
  if grep -qF 'replace this with' "$task_file"; then
    die "task briefing still contains metadata placeholders"
  fi

  implementation_intent=$(sed -n 's/^<!-- worktree-task-implementation-intent: \([^ ]*\) -->$/\1/p' "$task_file" | head -n 1)
  case "$implementation_intent" in
    requested|not-requested|unclear) ;;
    *) die "task briefing has invalid implementation intent: $implementation_intent" ;;
  esac

  implementation_status=$(sed -n 's/^<!-- worktree-task-implementation-status: \([^ ]*\) -->$/\1/p' "$task_file" | head -n 1)
  case "$implementation_status" in
    not-started|in-progress|blocked|completed) ;;
    *) die "task briefing has invalid implementation status: $implementation_status" ;;
  esac

  next_action=$(sed -n 's/^<!-- worktree-task-next-action: \([^ ]*\) -->$/\1/p' "$task_file" | head -n 1)
  case "$next_action" in
    implement|await-user|clarify) ;;
    *) die "task briefing has invalid next action: $next_action" ;;
  esac

  for section in \
    original-request \
    context-and-goal \
    confirmed-requirements \
    scope \
    behavior \
    constraints \
    assumptions \
    open-questions \
    execution-state \
    acceptance \
    references \
    handoff
  do
    validate_section "$task_file" "$section"
  done

  printf 'VALID: %s\n' "$task_file"
}

cmd_remove() {
  ticket_raw=$1
  ticket=$(sanitize_ticket "$ticket_raw")
  [ -n "$ticket" ] || die "ticket is required"

  root=$(project_root)
  worktree_dir="$root/.worktrees/$ticket"

  [ -e "$worktree_dir" ] || die "worktree not found: $worktree_dir"

  # worktree-task.md is generated by this skill and is untracked in the branch it was
  # created from, which otherwise makes `git worktree remove` refuse the whole worktree.
  # Only this known, self-generated file is cleared — any other uncommitted work still
  # blocks removal.
  rm -f "$worktree_dir/worktree-task.md"

  git -C "$root" worktree remove "$worktree_dir"
  printf 'REMOVED: %s\n' "$worktree_dir"
}

[ "$#" -ge 1 ] || { usage; exit 64; }

action=$1
shift

case "$action" in
  add)
    [ "$#" -ge 1 ] && [ "$#" -le 3 ] || { usage; exit 64; }
    cmd_add "$@"
    ;;
  validate)
    [ "$#" -eq 1 ] || { usage; exit 64; }
    cmd_validate "$@"
    ;;
  remove)
    [ "$#" -eq 1 ] || { usage; exit 64; }
    cmd_remove "$@"
    ;;
  *)
    usage
    exit 64
    ;;
esac
