---
name: dotnet-runner
description: |
  Execute any .NET CLI command with complete output capture, compact structured
  reporting, and optional read-only log analysis. Use for dotnet build, test,
  run, publish, pack, restore, clean, tool, and other dotnet CLI operations.
  The workflow preserves arbitrary command arguments and supports finite and
  long-running processes without flooding the calling agent with raw logs.
  Trigger phrases: "dotnet build", "dotnet test", "dotnet run", "dotnet publish",
  "dotnet pack", "dotnet restore", "dotnet clean", "dotnet CLI", "run a .NET command",
  "build the project", "run tests", "publish the app", "build 專案", "跑測試",
  "執行測試", "執行 dotnet".
license: MIT
metadata:
  author: aa89227
  version: "2.3"
  tags: ["dotnet", "cli", "build", "test", "run", "automation", "csharp", "logging"]
  trigger_keywords: ["dotnet build", "dotnet test", "dotnet run", "dotnet publish",
    "dotnet pack", "dotnet restore", "dotnet clean", "dotnet CLI", "run a .NET command",
    "build project", "run tests", "publish app", "build 專案", "跑測試", "執行測試",
    "執行 dotnet"]
---

# dotnet-runner Skill

## Purpose

Use this skill for every .NET CLI operation. This is a command runner, not a
test-only workflow. Its primary responsibility is to preserve complete process
output while returning only the amount of information the calling agent needs.

The command arguments are owned by the caller. The runner must not rewrite,
remove, reorder, or inject .NET CLI options in order to reduce output. Output
reduction happens after capture, through reporting and log analysis.

## Execution contract

### 1. Preserve the exact command

- Execute the requested `dotnet` command once with the supplied working
  directory and environment.
- Prefer a process API that accepts an argument vector instead of a shell string.
  This preserves spaces, quotes, `-p:` properties, repeated options, and every
  argument after `--`.
- If a shell is unavoidable, quote the original command without changing its
  arguments.
- Do not add `--verbosity quiet`, `--no-restore`, `--no-build`, `--logger`, or
  any other option unless the caller explicitly requested it.
- Run multi-command workflows as separate invocations so every command has its
  own exit code, log, and report.

### 2. Choose an accessible artifact directory

Store the complete output in a per-run directory. Resolve the artifact root in
this order:

1. An explicit artifact directory supplied by the caller, such as
   `--artifact-dir DIR` or `DOTNET_RUNNER_ARTIFACT_DIR`.
2. An artifact or scratch directory supplied by the current harness.
3. `artifacts/dotnet-runner/<run-id>/` below the current workspace.

Do not default to `/tmp`, the user's home directory, `/var/log`, or a
host-specific agent directory. If no location is both writable by the process
and readable by the workflow, report the problem before starting the command.

Use relative artifact paths in the report whenever the workspace is known. A
typical run directory contains:

```text
artifacts/dotnet-runner/<run-id>/
├── output.log       # Complete stdout/stderr in execution order
├── report.json      # Machine-readable compact result
└── artifacts/       # Screenshots, dumps, reports, or other command outputs
```

Create a unique run directory and never overwrite a previous run by default.
Keep command metadata redacted; do not persist secrets from arguments or the
environment. The workspace artifact directory should normally be gitignored.

### 3. Use the bundled shell runner when native capture is unavailable

The skill includes `scripts/run-dotnet.sh` for hosts that do not expose a
native process-capture API. Invoke the script as the top-level command and pass
the complete .NET argument list after `--`:

```bash
scripts/run-dotnet.sh --artifact-dir artifacts/dotnet-runner -- test \
  tests/Example.Tests/ --filter-trait "Category=E2E"
```

The script starts `dotnet` with `dotnet "$@"`; it does not use `eval`, rebuild
the command as a shell string, or add a `tee` pipeline. In its default
`compact` mode it writes all output to `output.log`, emits a bounded summary,
creates `report.json`, and returns the original exit code. `tail` adds a
bounded post-run tail, while `full` explicitly prints the saved log.

When a host has a native process-capture API, prefer that API over the shell
runner. When a host applies command approval rules, allow the reviewed runner
entry point as a narrow capability; do not broaden approval to arbitrary shell
commands merely because the runner exists. The shell runner is an execution
adapter, not a way to bypass the host's security policy.

### 4. Capture without flooding the caller

- Merge stdout and stderr into the complete log while preserving their order
  as far as the host process API allows.
- The default output mode is `compact`: write the complete log, but return only
  the structured report and selected evidence.
- Do not use `tee` as the default capture strategy because it writes the log and
  forwards the entire stream to the calling agent.
- Never discard output. If the host supports live mirroring, make it an
  explicit output mode rather than silently enabling it.
- Always preserve the process exit code, termination signal, elapsed time, and
  whether the process timed out or was cancelled.

Supported output modes:

| Mode | Behavior |
| --- | --- |
| `compact` | Full log is saved; only summary and relevant evidence are returned. |
| `live` | Full log is saved and the complete stream is mirrored live. |
| `tail` | Full log is saved; only bounded recent output is mirrored periodically. |
| `full` | Full log is returned when the caller explicitly requests it. |

Use `compact` unless the caller asks for live debugging or the process is being
actively diagnosed.

## Reporting and analysis

Determine the operation from the .NET command verb when possible (`build`,
`test`, `run`, `restore`, `publish`, `pack`, `clean`, `tool`, and so on). Do not
create a branch for every possible combination of command-line options.

Use an operation-specific parser when the output format is known and a generic
parser otherwise. Parsers must tolerate different SDK versions, test runners,
and verbosity levels. If a parser cannot identify a field, report it as
unknown rather than guessing.

### Basic parser output

The parser should always produce:

- `status`: `success`, `failed`, `cancelled`, or `timed_out`
- exit code and elapsed time
- operation and target when identifiable
- concise summary metrics
- exact relevant error or warning messages
- log and artifact paths
- whether any report section was truncated

Operation-specific summaries normally include:

| Operation | Preferred summary |
| --- | --- |
| `build` | Projects, errors, warnings, file paths, line numbers, and error codes |
| `test` | Total, passed, failed, skipped, failed test names, assertions, and artifacts |
| `restore` | Package resolution, source, authentication, and compatibility failures |
| `run` | Process state, readiness or endpoint evidence, important errors, and termination |
| `publish` / `pack` | Result, warnings/errors, and produced package or publish paths |
| `clean` | Result, errors, and affected target when available |
| Other commands | Exit status, duration, important error lines, and bounded final output |

### Optional read-only log analysis

Prefer a read-only log-analysis worker when the host provides one, but do not
make a particular agent product or sub-agent API a requirement of this skill.
The command runner remains responsible for starting and supervising the
process. The analysis worker only reads the completed log and report.

Use the log-analysis worker when:

- the command fails;
- the output is large or its format is not recognized;
- several failures must be correlated;
- the result may be an environment, container, network, timeout, or assertion
  problem; or
- the caller requests detailed diagnostics.

For a successful command with a recognized summary, the basic parser is enough;
do not spend an additional analysis step merely to repeat a clean result.

The log-analysis worker must return a bounded, evidence-based report:

1. Overall result and relevant counts.
2. Every failed item that can be identified, not only the first one.
3. Exact error, assertion, or exception messages.
4. Classification such as build, assertion, timeout, dependency, container,
   network, or unknown environment failure.
5. Relevant log line ranges and artifact paths.
6. A clear indication when the log is incomplete or the format is ambiguous.

The worker must not execute commands, rerun the operation, modify files, or
invent details that are not supported by the log. The main agent should receive
the report and selected evidence, not the entire raw log. The raw log remains
available for an explicit follow-up investigation.

## Result format

Return a compact result in a stable structure similar to:

```text
status: failed
operation: test
exit_code: 1
duration: 2m41s

summary:
  total: 30
  passed: 29
  failed: 1
  skipped: 0

issues:
  - kind: visual-regression
    name: ArticleListLoads
    message: <exact message>
    evidence: output.log:412-428

log: artifacts/dotnet-runner/<run-id>/output.log
report: artifacts/dotnet-runner/<run-id>/report.json
artifacts:
  - artifacts/dotnet-runner/<run-id>/artifacts/received.png
```

Do not print the complete log for a successful command. For failures, include
exact messages and bounded context; provide the log path and line ranges for
the remaining details. If the caller asks for the full log, return it in
manageable sections or let the caller read the artifact directly.

## Long-running commands

`dotnet run` and custom tools may intentionally remain active. Do not assume
that a process which has not exited has failed.

- Use `live` or `tail` mode when ongoing output is useful.
- Use an explicit timeout or lifecycle instruction before terminating a
  long-running process.
- Report startup, readiness, endpoint, termination, and timeout events without
  forwarding every routine log line.
- Keep the log accessible while the process is running and after it exits.

## Failure handling

- Preserve the original exit code even when log parsing fails.
- Do not automatically rerun a failed command unless the caller asks for a
  rerun or the workflow explicitly defines a retry policy.
- Distinguish command failure from analysis failure. A parser error must not be
  reported as a failed .NET command.
- When the output is too large for the current response, reduce the report, not
  the saved log.
- When an artifact cannot be read by the current harness, report its path and
  the access limitation instead of copying unbounded data into the response.

## Portability requirements

This skill must remain independent of any particular agent host. Do not require
Codex sandbox rules, approval policies, a named sub-agent type, a session
scratchpad variable, or a host-specific filesystem path. Hosts may provide
their own process executor, artifact sink, log parser, or read-only analysis
worker, but the capture and reporting contract remains the same.

## Principles

- Execute the exact requested command once.
- Preserve all output in an accessible artifact.
- Keep the main agent's context small by default.
- Prefer deterministic parsing, with read-only log analysis for difficult cases.
- Return exact evidence rather than a vague paraphrase.
- Never trade command correctness for output reduction.
