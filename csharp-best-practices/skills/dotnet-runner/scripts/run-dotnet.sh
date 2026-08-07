#!/bin/sh

# Run dotnet without a shell pipeline. The complete process output is kept in
# an artifact file; only a bounded report is written to the caller's stdout.
set -eu

artifact_root="${DOTNET_RUNNER_ARTIFACT_DIR:-artifacts/dotnet-runner}"
mode="compact"

usage() {
    cat >&2 <<'EOF'
Usage: run-dotnet.sh [--artifact-dir DIR] [--mode compact|tail|full] -- <dotnet arguments...>

The command after -- is passed to dotnet unchanged. The default mode saves the
complete output and prints only a bounded summary.
EOF
}

while [ "$#" -gt 0 ]; do
    case "$1" in
        --artifact-dir)
            [ "$#" -ge 2 ] || { usage; exit 64; }
            artifact_root=$2
            shift 2
            ;;
        --artifact-dir=*)
            artifact_root=${1#--artifact-dir=}
            shift
            ;;
        --mode)
            [ "$#" -ge 2 ] || { usage; exit 64; }
            mode=$2
            shift 2
            ;;
        --mode=*)
            mode=${1#--mode=}
            shift
            ;;
        --)
            shift
            break
            ;;
        *)
            break
            ;;
    esac
done

case "$mode" in
    compact|tail|full) ;;
    *)
        usage
        exit 64
        ;;
esac

[ "$#" -gt 0 ] || { usage; exit 64; }

umask 077
mkdir -p "$artifact_root"

timestamp=$(date -u +%Y%m%dT%H%M%SZ)
run_id="$timestamp-$$"
run_dir="$artifact_root/$run_id"
attempt=0
while ! mkdir "$run_dir" 2>/dev/null; do
    attempt=$((attempt + 1))
    run_dir="$artifact_root/$run_id-$attempt"
done

log_file="$run_dir/output.log"
report_file="$run_dir/report.json"
operation=$1
started=$(date -u +%s)

set +e
dotnet "$@" >"$log_file" 2>&1
exit_code=$?
set -e

finished=$(date -u +%s)
duration_seconds=$((finished - started))

case "$exit_code" in
    0) status="success" ;;
    *) status="failed" ;;
esac

# Paths and operation names come from command metadata rather than process
# output. They are restricted to ordinary single-line values in this report.
json_escape() {
    printf '%s' "$1" | sed 's/\\/\\\\/g; s/"/\\"/g'
}

escaped_status=$(json_escape "$status")
escaped_operation=$(json_escape "$operation")
escaped_log_file=$(json_escape "$log_file")
cat >"$report_file" <<EOF
{
  "status": "$escaped_status",
  "operation": "$escaped_operation",
  "exitCode": $exit_code,
  "durationSeconds": $duration_seconds,
  "logPath": "$escaped_log_file"
}
EOF

printf 'dotnet-runner: status=%s exit_code=%s duration=%ss\n' "$status" "$exit_code" "$duration_seconds"
printf 'dotnet-runner: log=%s\n' "$log_file"
printf 'dotnet-runner: report=%s\n' "$report_file"

if [ "$mode" = "full" ]; then
    cat "$log_file"
    exit "$exit_code"
fi

printf 'dotnet-runner: recognized output:\n'
recognized=0
while IFS= read -r line; do
    printf '%s\n' "$line"
    recognized=$((recognized + 1))
    [ "$recognized" -ge 40 ] && break
done <<EOF
$(awk '
    /Build succeeded|Build FAILED|Test run|Total tests:|Passed:|Failed:|Skipped:|error[[:space:]]+[A-Z]+[0-9]+|Unhandled exception|Exception:/ { print }
' "$log_file")
EOF

if [ "$recognized" -eq 0 ]; then
    printf '%s\n' '  (no standard summary was detected; inspect the saved log for details)'
fi

if [ "$mode" = "tail" ] || [ "$exit_code" -ne 0 ]; then
    printf 'dotnet-runner: last output lines:\n'
    tail -n 60 "$log_file"
fi

exit "$exit_code"
