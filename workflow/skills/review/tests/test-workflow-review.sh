#!/usr/bin/env bash
set -euo pipefail

readonly repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd -P)"
readonly test_root="$(mktemp -d /tmp/workflow-review-test.XXXXXX)"
trap 'rm -rf -- "$test_root"' EXIT

mkdir -p "$test_root/bin"
package_bin="$(jq -r '.bin["workflow-review"] // empty' "$repo_root/package.json")"
[[ "$package_bin" = ./workflow/skills/review/scripts/workflow-review ]]
package_bin_path="$repo_root/${package_bin#./}"
[[ -x "$package_bin_path" ]]
ln -s "$package_bin_path" "$test_root/bin/workflow-review"
export PATH="$test_root/bin:$PATH"
readonly review_command="workflow-review"
command -v "$review_command" >/dev/null

mkdir -p "$test_root/project/.workflow/review-rules"

printf '%s\n' \
  '---' \
  'type: review-rule' \
  'id: architecture-boundary' \
  'name: Architecture Boundary' \
  'enabled: true' \
  'required: true' \
  'steps: identify-change-surface,check-boundary,write-report' \
  'dynamic_parents: check-boundary' \
  '---' \
  '# Architecture Boundary' \
  '' \
  'Check the dependency direction.' \
  > "$test_root/project/.workflow/review-rules/architecture-boundary.md"

printf '%s\n' \
  '---' \
  'type: review-rule' \
  'id: documentation-quality' \
  'name: Documentation Quality' \
  'enabled: true' \
  'required: false' \
  'steps: inspect-documentation,write-report' \
  'dynamic_parents: ' \
  '---' \
  '# Documentation Quality' \
  '' \
  'Check user-facing documentation.' \
  > "$test_root/project/.workflow/review-rules/documentation-quality.md"

printf '%s\n' 'initial tracked content' > "$test_root/project/tracked.txt"

git -C "$test_root/project" init -q
git -C "$test_root/project" config user.email review-test@example.invalid
git -C "$test_root/project" config user.name review-test
git -C "$test_root/project" add .
git -C "$test_root/project" commit -qm initial

ln -s "$(pwd -P)" "$test_root/outside-link"
if WORKFLOW_REVIEW_ROOT="$test_root/outside-link/new" "$review_command" start \
  --project-root "$test_root/project" --rule-id architecture-boundary \
  >/dev/null 2>"$test_root/unsafe-root.err"; then
  printf 'start accepted a review root whose existing parent resolves outside /tmp\n' >&2
  exit 1
fi
grep -q 'outside /tmp' "$test_root/unsafe-root.err"
[[ ! -e "$test_root/outside-link/new" ]]

trailing_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
trailing_review_id="$(jq -r '.review_id' <<< "$trailing_start")"
trailing_artifact_dir="$(jq -r '.artifact_dir' <<< "$trailing_start")"
[[ "$trailing_artifact_dir" != *//review-* ]]
trailing_task="$({
  WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" get-task \
    --review-id "$trailing_review_id" --rule-id architecture-boundary
})"
trailing_task_id="$(jq -r '.task_id' <<< "$trailing_task")"
trailing_report="$(jq -r '.report_path' <<< "$trailing_task")"
while IFS= read -r trailing_step_id; do
  WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" start-step \
    --task-id "$trailing_task_id" --step-id "$trailing_step_id" >/dev/null
  WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" complete-step \
    --task-id "$trailing_task_id" --step-id "$trailing_step_id" >/dev/null
done < <(jq -r '.steps[].step_id' <<< "$trailing_task")
printf '%s\n' '# Review Report' '' 'Result: PASS' > "$trailing_report"
WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" submit \
  --task-id "$trailing_task_id" --status completed >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" wait \
  --review-id "$trailing_review_id" --timeout-seconds 1 >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/trailing-runs///" "$review_command" cleanup \
  --review-id "$trailing_review_id" >/dev/null

if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
  --project-root "$test_root/project" --rule-id documentation-quality \
  >/dev/null 2>"$test_root/required-rule.err"; then
  printf 'start omitted an enabled required rule\n' >&2
  exit 1
fi
grep -q 'enabled required rule was not selected' "$test_root/required-rule.err"

start_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" \
    --rule-id architecture-boundary \
    --rule-id documentation-quality
})"
review_id="$(jq -r '.review_id' <<< "$start_json")"
artifact_dir="$(jq -r '.artifact_dir' <<< "$start_json")"

jq -e '.rules | length == 2' <<< "$start_json" >/dev/null

task_one="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
    --review-id "$review_id" --rule-id architecture-boundary
})"
task_one_id="$(jq -r '.task_id' <<< "$task_one")"
report_one="$(jq -r '.report_path' <<< "$task_one")"
jq -e '.rule | contains("dependency direction")' <<< "$task_one" >/dev/null
jq -e '.steps | length == 3 and .[0].step_id == "identify-change-surface" and .[1].step_id == "check-boundary"' \
  <<< "$task_one" >/dev/null

if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$task_one_id" --status completed >/dev/null 2>&1; then
  printf 'submit accepted a missing report\n' >&2
  exit 1
fi

WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$task_one_id" --step-id identify-change-surface >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$task_one_id" --step-id identify-change-surface >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$task_one_id" --step-id check-boundary >/dev/null
dynamic_step_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" add-step \
    --task-id "$task_one_id" \
    --parent-step-id check-boundary \
    --step-id inspect-dependent-boundary \
    --reason 'A new dependency crosses the reviewed boundary.'
})"
jq -e '.step.parent_step_id == "check-boundary" and .step.state == "pending"' \
  <<< "$dynamic_step_json" >/dev/null
jq -e '.rules[0].current_step == "check-boundary" and ([.rules[0].steps[] | select(.step_id == "inspect-dependent-boundary") | .state] | .[0] == "pending")' \
  <<< "$(WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" status --review-id "$review_id")" >/dev/null
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$task_one_id" --step-id check-boundary >/dev/null 2>&1; then
  printf 'complete-step accepted a parent with a pending dynamic child\n' >&2
  exit 1
fi
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$task_one_id" --step-id inspect-dependent-boundary >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$task_one_id" --step-id inspect-dependent-boundary >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$task_one_id" --step-id check-boundary >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$task_one_id" --step-id write-report >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$task_one_id" --step-id write-report >/dev/null
printf '%s\n' '# Review Report' '' 'Result: PASS' '' '### Blocking' '' '- None' > "$report_one"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$task_one_id" --status completed >/dev/null

if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$review_id" --rule-id architecture-boundary >/dev/null 2>&1; then
  printf 'get-task allowed a duplicate reviewer claim\n' >&2
  exit 1
fi
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$task_one_id" --status failed >/dev/null 2>&1; then
  printf 'submit rewrote a terminal task\n' >&2
  exit 1
fi

task_two="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
    --review-id "$review_id" --rule-id documentation-quality
})"
task_two_id="$(jq -r '.task_id' <<< "$task_two")"
report_two="$(jq -r '.report_path' <<< "$task_two")"
while IFS= read -r task_two_step_id; do
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
    --task-id "$task_two_id" --step-id "$task_two_step_id" >/dev/null
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
    --task-id "$task_two_id" --step-id "$task_two_step_id" >/dev/null
done < <(jq -r '.steps[].step_id' <<< "$task_two")
printf '%s\n' '# Review Report' '' 'Result: PASS' '' '### Blocking' '' '- None' > "$report_two"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$task_two_id" --status completed >/dev/null

wait_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$review_id" --timeout-seconds 5
})"
jq -e '.complete == true and .all_tasks_terminal == true and .timed_out == false and .review_gate == "requires-report-aggregation" and ([.rules[].state] | all(. == "completed"))' \
  <<< "$wait_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$review_id" >/dev/null
[[ ! -d "$artifact_dir" ]]

blocked_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
blocked_review_id="$(jq -r '.review_id' <<< "$blocked_start")"
blocked_task="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
    --review-id "$blocked_review_id" --rule-id architecture-boundary
})"
blocked_task_id="$(jq -r '.task_id' <<< "$blocked_task")"
blocked_report="$(jq -r '.report_path' <<< "$blocked_task")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$blocked_task_id" --step-id identify-change-surface >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$blocked_task_id" --step-id identify-change-surface >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$blocked_task_id" --step-id check-boundary >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" add-step \
  --task-id "$blocked_task_id" \
  --parent-step-id check-boundary \
  --step-id unavailable-dependent-check \
  --reason 'The dependent service is unavailable.' >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
  --task-id "$blocked_task_id" --step-id unavailable-dependent-check >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
  --task-id "$blocked_task_id" --step-id unavailable-dependent-check \
  --status blocked --reason 'The dependent service is unavailable.' >/dev/null
printf '%s\n' '# Review Report' '' 'Result: BLOCKED' > "$blocked_report"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$blocked_task_id" --status blocked >/dev/null
blocked_wait="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$blocked_review_id" --timeout-seconds 1
})"
jq -e '.complete == true and .review_gate == "blocked" and .rules[0].state == "blocked"' \
  <<< "$blocked_wait" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$blocked_review_id" >/dev/null

printf '%s\n' 'dirty before review' > "$test_root/project/tracked.txt"
source_change_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
source_change_review_id="$(jq -r '.review_id' <<< "$source_change_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$source_change_review_id" --rule-id architecture-boundary >/dev/null
printf '%s\n' 'dirty after review' > "$test_root/project/tracked.txt"

source_change_json=''
source_change_status=0
if source_change_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$source_change_review_id" --timeout-seconds 0
})"; then
  printf 'wait ignored a changed review target\n' >&2
  exit 1
else
  source_change_status=$?
fi
[[ "$source_change_status" -eq 3 ]]
jq -e '.complete == false and .task_result == "source-changed" and .review_gate == "blocked"' \
  <<< "$source_change_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$source_change_review_id" >/dev/null

printf '%s\n' 'untracked before review' > "$test_root/project/untracked.txt"
untracked_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
untracked_review_id="$(jq -r '.review_id' <<< "$untracked_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$untracked_review_id" --rule-id architecture-boundary >/dev/null
printf '%s\n' 'untracked after review' > "$test_root/project/untracked.txt"

untracked_json=''
untracked_status=0
if untracked_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$untracked_review_id" --timeout-seconds 0
})"; then
  printf 'wait ignored a changed untracked review target\n' >&2
  exit 1
else
  untracked_status=$?
fi
[[ "$untracked_status" -eq 3 ]]
jq -e '.complete == false and .task_result == "source-changed" and .review_gate == "blocked"' \
  <<< "$untracked_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$untracked_review_id" >/dev/null

mkdir -p "$test_root/non-git-project/.workflow/review-rules"
cp "$test_root/project/.workflow/review-rules/architecture-boundary.md" \
  "$test_root/non-git-project/.workflow/review-rules/architecture-boundary.md"
printf '%s\n' 'non-git initial content' > "$test_root/non-git-project/source.txt"
non_git_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/non-git-project" --rule-id architecture-boundary
})"
non_git_review_id="$(jq -r '.review_id' <<< "$non_git_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$non_git_review_id" --rule-id architecture-boundary >/dev/null
printf '%s\n' 'non-git changed content' > "$test_root/non-git-project/source.txt"

non_git_json=''
non_git_status=0
if non_git_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$non_git_review_id" --timeout-seconds 0
})"; then
  printf 'wait ignored a changed non-Git review target\n' >&2
  exit 1
else
  non_git_status=$?
fi
[[ "$non_git_status" -eq 3 ]]
jq -e '.complete == false and .task_result == "source-changed" and .review_gate == "blocked"' \
  <<< "$non_git_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$non_git_review_id" >/dev/null

timeout_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
timeout_review_id="$(jq -r '.review_id' <<< "$timeout_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$timeout_review_id" --rule-id architecture-boundary >/dev/null

timeout_json=''
timeout_status=0
if timeout_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$timeout_review_id" --timeout-seconds 0
})"; then
  printf 'wait unexpectedly completed a running task\n' >&2
  exit 1
else
  timeout_status=$?
fi
[[ "$timeout_status" -eq 2 ]]
jq -e '.complete == false and .timed_out == true and .rules[0].state == "running"' \
  <<< "$timeout_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$timeout_review_id" >/dev/null

printf 'workflow-review tests passed\n'
