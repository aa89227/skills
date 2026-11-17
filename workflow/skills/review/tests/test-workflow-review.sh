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
  --review-id "$trailing_review_id" --timeout-seconds 600 >/dev/null
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

concurrent_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
concurrent_review_id="$(jq -r '.review_id' <<< "$concurrent_start")"
concurrent_artifact_dir="$(jq -r '.artifact_dir' <<< "$concurrent_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$concurrent_review_id" --rule-id architecture-boundary >/dev/null
concurrent_wait_output="$test_root/concurrent-wait.json"
concurrent_wait_error="$test_root/concurrent-wait.err"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
  --review-id "$concurrent_review_id" --timeout-seconds 7200 \
  >"$concurrent_wait_output" 2>"$concurrent_wait_error" &
concurrent_wait_pid=$!
for _ in {1..20}; do
  [[ -f "$concurrent_artifact_dir/wait.env" ]] && break
  sleep 0.1
done
[[ -f "$concurrent_artifact_dir/wait.env" ]]
concurrent_status_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" status \
    --review-id "$concurrent_review_id"
})"
jq -e '.wait.timeout_seconds == 7200 and .wait.clock_source == "monotonic" and .wait.state == "running"' \
  <<< "$concurrent_status_json" >/dev/null
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
  --review-id "$concurrent_review_id" --timeout-seconds 600 \
  >/dev/null 2>"$test_root/second-wait.err"; then
  printf 'second wait process was allowed to share a running review\n' >&2
  kill "$concurrent_wait_pid" 2>/dev/null || true
  wait "$concurrent_wait_pid" 2>/dev/null || true
  exit 1
fi
grep -q 'another wait process is already active' "$test_root/second-wait.err"
kill "$concurrent_wait_pid" 2>/dev/null || true
wait "$concurrent_wait_pid" 2>/dev/null || true
for short_timeout in 60 300; do
  if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
    --review-id "$concurrent_review_id" --timeout-seconds "$short_timeout" \
    >/dev/null 2>"$test_root/short-timeout-$short_timeout.err"; then
    printf 'wait accepted a timeout below the ten-minute policy floor\n' >&2
    exit 1
  fi
  grep -q 'timeout must be at least 600 seconds (10 minutes)' \
    "$test_root/short-timeout-$short_timeout.err"
done

clock_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
clock_review_id="$(jq -r '.review_id' <<< "$clock_start")"
clock_artifact_dir="$(jq -r '.artifact_dir' <<< "$clock_start")"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
  --review-id "$clock_review_id" --rule-id architecture-boundary >/dev/null
clock_date_bin="$test_root/fake-date-bin"
mkdir -p "$clock_date_bin"
export WORKFLOW_REVIEW_FAKE_DATE_MARKER="$test_root/fake-date.marker"
printf '%s\n' \
  '#!/usr/bin/env bash' \
  'if [[ "$1" = "+%s" ]]; then' \
  '  if [[ -e "$WORKFLOW_REVIEW_FAKE_DATE_MARKER" ]]; then' \
  '    printf "%s\\n" 9999999999' \
  '  else' \
  '    : > "$WORKFLOW_REVIEW_FAKE_DATE_MARKER"' \
  '    printf "%s\\n" 1000' \
  '  fi' \
  '  exit 0' \
  'fi' \
  'exec /bin/date "$@"' > "$clock_date_bin/date"
chmod +x "$clock_date_bin/date"
PATH="$clock_date_bin:$PATH" WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
  --review-id "$clock_review_id" --timeout-seconds 600 \
  >"$test_root/clock-wait.json" 2>"$test_root/clock-wait.err" &
clock_wait_pid=$!
for _ in {1..20}; do
  [[ -f "$clock_artifact_dir/wait.env" ]] && break
  sleep 0.1
done
[[ -f "$clock_artifact_dir/wait.env" ]]
sleep 2
clock_status_json="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" status \
    --review-id "$clock_review_id"
})"
jq -e '.state == "running" and .wait.clock_source == "monotonic" and .wait.state == "running"' \
  <<< "$clock_status_json" >/dev/null
kill "$clock_wait_pid" 2>/dev/null || true
wait "$clock_wait_pid" 2>/dev/null || true

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
    --review-id "$review_id" --timeout-seconds 600
})"
jq -e '.complete == true and .all_tasks_terminal == true and .timed_out == false and .review_gate == "requires-report-aggregation" and ([.rules[].state] | all(. == "completed"))' \
  <<< "$wait_json" >/dev/null
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$review_id" >/dev/null
[[ ! -d "$artifact_dir" ]]

retained_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" \
    --rule-id architecture-boundary \
    --rule-id documentation-quality
})"
retained_review_id="$(jq -r '.review_id' <<< "$retained_start")"
retained_artifact_dir="$(jq -r '.artifact_dir' <<< "$retained_start")"
retained_task="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
    --review-id "$retained_review_id" --rule-id architecture-boundary
})"
retained_task_id="$(jq -r '.task_id' <<< "$retained_task")"
retained_report="$(jq -r '.report_path' <<< "$retained_task")"
while IFS= read -r retained_step_id; do
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
    --task-id "$retained_task_id" --step-id "$retained_step_id" >/dev/null
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
    --task-id "$retained_task_id" --step-id "$retained_step_id" >/dev/null
done < <(jq -r '.steps[].step_id' <<< "$retained_task")
printf '%s\n' '# Review Report' '' 'Result: PASS' > "$retained_report"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$retained_task_id" --status completed >/dev/null
sed -i 's/^state=.*/state=timed_out/' "$retained_artifact_dir/run.env"
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$retained_review_id" >/dev/null 2>"$test_root/early-cleanup.err"; then
  printf 'cleanup deleted a timed-out run before report aggregation\n' >&2
  exit 1
fi
grep -q 'before report aggregation' "$test_root/early-cleanup.err"
[[ -f "$retained_report" ]]
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$retained_review_id" --after-aggregation \
  >/dev/null 2>"$test_root/incomplete-cleanup.err"; then
  printf 'cleanup deleted a timed-out run with a non-terminal task\n' >&2
  exit 1
fi
grep -q 'before every task is terminal' "$test_root/incomplete-cleanup.err"
[[ -f "$retained_report" ]]
grep -q 'Result: PASS' "$retained_report"

retained_complete_start="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start \
    --project-root "$test_root/project" --rule-id architecture-boundary
})"
retained_complete_review_id="$(jq -r '.review_id' <<< "$retained_complete_start")"
retained_complete_artifact_dir="$(jq -r '.artifact_dir' <<< "$retained_complete_start")"
retained_complete_task="$({
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" get-task \
    --review-id "$retained_complete_review_id" --rule-id architecture-boundary
})"
retained_complete_task_id="$(jq -r '.task_id' <<< "$retained_complete_task")"
retained_complete_report="$(jq -r '.report_path' <<< "$retained_complete_task")"
while IFS= read -r retained_complete_step_id; do
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" start-step \
    --task-id "$retained_complete_task_id" --step-id "$retained_complete_step_id" >/dev/null
  WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" complete-step \
    --task-id "$retained_complete_task_id" --step-id "$retained_complete_step_id" >/dev/null
done < <(jq -r '.steps[].step_id' <<< "$retained_complete_task")
printf '%s\n' '# Review Report' '' 'Result: PASS' > "$retained_complete_report"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" submit \
  --task-id "$retained_complete_task_id" --status completed >/dev/null
sed -i 's/^state=.*/state=timed_out/' "$retained_complete_artifact_dir/run.env"
if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$retained_complete_review_id" >/dev/null 2>"$test_root/complete-early-cleanup.err"; then
  printf 'cleanup deleted a timed-out report before aggregation\n' >&2
  exit 1
fi
grep -q 'before report aggregation' "$test_root/complete-early-cleanup.err"
[[ -f "$retained_complete_report" ]]
grep -q 'Result: PASS' "$retained_complete_report"
WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" cleanup \
  --review-id "$retained_complete_review_id" --after-aggregation >/dev/null
[[ ! -d "$retained_complete_artifact_dir" ]]

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
    --review-id "$blocked_review_id" --timeout-seconds 600
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
    --review-id "$source_change_review_id" --timeout-seconds 600
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
  --review-id "$source_change_review_id" --after-aggregation >/dev/null 2>&1 || true

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
    --review-id "$untracked_review_id" --timeout-seconds 600
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
  --review-id "$untracked_review_id" --after-aggregation >/dev/null 2>&1 || true

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
    --review-id "$non_git_review_id" --timeout-seconds 600
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
  --review-id "$non_git_review_id" --after-aggregation >/dev/null 2>&1 || true

if WORKFLOW_REVIEW_ROOT="$test_root/runs" "$review_command" wait \
  --review-id review-0000000000000000 --timeout-seconds 599 \
  >/dev/null 2>"$test_root/short-timeout.err"; then
  printf 'wait accepted a timeout shorter than the minimum\n' >&2
  exit 1
fi
grep -q 'timeout must be at least 600 seconds (10 minutes)' "$test_root/short-timeout.err"

printf 'workflow-review tests passed\n'
