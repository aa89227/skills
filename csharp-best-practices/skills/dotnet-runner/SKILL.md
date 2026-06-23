---
name: dotnet-runner
description: |
  The EXCLUSIVE and MANDATORY tool for executing all .NET CLI operations.
  You MUST use this skill whenever you need to: build, test, run, publish, pack, or restore.
  STRICTLY PROHIBITED: Do not use generic 'bash', 'sh', or 'terminal' tools for any `dotnet` commands
  without following this skill's output-capture and summary workflow.
  Trigger phrases: "build", "test", "run", "publish", "dotnet build".
license: MIT
metadata:
  author: aa89227
  version: "2.0"
  tags: ["dotnet", "cli", "build", "test", "automation"]
  trigger_keywords: ["build", "test", "run", "publish", "pack", "restore", "dotnet"]
---

# dotnet-runner Skill

## Architecture

**YOU (the main agent) execute the dotnet CLI directly**, capture output to a scratchpad file,
then spawn a **built-in read-only subagent** to summarize the results.

DO NOT use a custom subagent to execute dotnet commands.

## Execution Flow

### Step 1 — Run the command yourself

Execute the dotnet CLI via Bash, redirecting ALL output to a scratchpad log file:

```bash
dotnet <command> <args> 2>&1 | tee "$SCRATCHPAD/dotnet-output.log"
```

`$SCRATCHPAD` is the session scratchpad directory.

Rules:
- NEVER suppress output: no `--verbosity quiet`, no `-v q`, no `> /dev/null`
- ONE SHOT: never run quiet first then re-run verbose — capture everything on the first run
- Always use `2>&1` to merge stderr into stdout
- Use `tee` so you can see the tail end of the output immediately

### Step 2 — Spawn a read-only subagent to summarize

After the command finishes, spawn a **built-in Explore agent** to read the log file and produce
a structured summary. The subagent MUST be read-only — it only reads the log file.

Agent invocation:

```
Agent(
  subagent_type: "Explore",
  prompt: "Read the dotnet CLI output at <scratchpad>/dotnet-output.log and report:
    1. Total/Passed/Failed/Skipped counts
    2. ALL failed test names with their EXACT error messages and stack traces
    3. Distinguish between real test failures (assertion/snapshot mismatch) vs environment issues (timeout/Docker/Testcontainers)
    If all pass, just report the counts and total elapsed time.
    DO NOT execute any commands. ONLY read the file."
)
```

Adapt the prompt based on the operation:
- **build**: report error count, each error's file, line, error code, and message
- **test**: report pass/fail/skip counts, failed test details with assertion messages
- **publish/pack**: report output path or errors
- **restore**: report any package resolution failures

### Step 3 — Act on the summary

- **All passed**: report the concise summary to the user
- **Failures**: analyze the subagent's summary, then fix issues or report to the user

## When to Invoke This Workflow

1. **User explicitly requests CLI operations:**
   - "build the project"
   - "run tests"
   - "publish the app"

2. **After code changes that require validation:**
   - Completed implementing a feature → build
   - Fixed compilation errors → verify build
   - Updated dependencies → restore and test

3. **Multi-step workflows:**
   - "Build and test" → run both, capture both outputs
   - "Restore, build, and run" → chain commands

## Supported Commands

| Operation | Command | Common Scenarios |
|-----------|---------|------------------|
| build | `dotnet build` | After code changes, verifying fixes |
| test | `dotnet test` | After implementation, CI/CD validation |
| run | `dotnet run` | Local testing, debugging |
| publish | `dotnet publish` | Release preparation |
| pack | `dotnet pack` | Library distribution |
| restore | `dotnet restore` | After updating `.csproj` |
| clean | `dotnet clean` | Before clean rebuild |

## Key Principles

- **You execute, subagent summarizes** — never delegate CLI execution to a subagent
- **Explore agent is read-only** — it only reads the log file, never runs commands
- **Proactive invocation** — don't ask "should I build?" when it's clearly needed
- **One-shot capture** — first run must have full output for diagnosis
