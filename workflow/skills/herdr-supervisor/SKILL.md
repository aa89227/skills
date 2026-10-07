---
name: herdr-supervisor
description: |
  Use only when the user explicitly invokes this skill to delegate work through HERDR.
  Act as a dispatch-only supervisor: assign named HERDR agents for implementation,
  investigation, or explicitly requested direct-dialogue planning, then stop managing them
  until the user gives a new explicit instruction. Do not use native subagents or the regular
  Worktree skills.
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["herdr", "supervisor", "delegation", "agent", "worktree", "workflow"]
---

# HERDR Supervisor

Use this skill only after the user explicitly invokes it. It turns the current agent into a
dispatch-only supervisor. The supervisor may inspect repository context read-only, classify the
request, choose a delegation shape, and send an initial task to a HERDR agent. It must not
implement the task itself or use the harness's native subagent/delegation capability.

## Enter supervisor mode

1. Verify the runtime before controlling HERDR:

   ```bash
   test "${HERDR_ENV:-}" = 1
   ```

   If the check fails, report that the current agent is not running inside a HERDR-managed pane
   and stop. Do not fall back to native subagents, direct implementation, or manual Git worktree
   commands.

2. Read the installed HERDR instructions before controlling panes, agents, or worktrees:

   ```bash
   herdr --skill
   ```

   Treat the installed CLI and its skill output as authoritative. Re-check command syntax with
   `herdr --help` or the relevant command group when needed; do not predict IDs or reuse stale
   pane/workspace identifiers.

3. If the user has not specified an agent kind, ask before creating a pane, agent, or Worktree.
   Do not silently choose `codex` or another default. Use the kind the user selects from the
   currently supported HERDR list.

## Dispatch contract

The supervisor is not an implementation agent. It may do the following before dispatch:

- inspect the current repository read-only to understand scope and choose a useful task prompt;
- classify the request as implementation, investigation, or direct-dialogue planning;
- create the HERDR layout and Worktree required by that classification;
- generate a short, semantic, unique agent name;
- send one initial prompt to the named agent.

It must not edit repository files, implement a delegated task, use a native subagent tool, or
silently turn a planning conversation into implementation.

Every live agent needs a human-readable name. Generate a short name from role and scope, such as
`issue-plan`, `api-auth`, or `test-review`. Names must satisfy HERDR's current constraints (the
installed instructions currently require a lowercase initial character and at most 32 characters)
and must be unique among live agents. Inspect live names before starting an agent; if there is a
collision, choose another concise name. Tell the user the assigned name so it can be used in later
explicit requests.

## Choose the delegation mode

### Implementation

Use this mode for any task that changes repository files, adds code, changes configuration, or
otherwise needs an implementation workspace.

- This mode is available only after the user explicitly invoked this skill.
- Create or open the Worktree with HERDR before starting the agent. Use `herdr worktree create`
  or `herdr worktree open`, not `workflow:worktree`, `git-worktrees`, or direct `git worktree`.
- Preserve the user's focus with `--no-focus` unless the user explicitly asks to switch focus.
- Pass user-provided branch, base, path, and label values exactly. Let HERDR choose omitted
  defaults rather than inventing a second Worktree convention.
- Parse the returned workspace, pane, and Worktree identifiers from HERDR's JSON response. Do not
  derive them from sidebar order or example IDs.
- Start the named agent in an available shell pane in that Worktree, then send the implementation
  brief without `--wait`.

The implementation prompt must identify the requested outcome, scope, repository/worktree
location, important constraints, and the instruction not to spawn or monitor other agents.

### Investigation

Use this mode for read-only research, repository inspection, diagnosis, review, or planning that
does not modify files.

- Do not create a Worktree unless the user explicitly requests one.
- Use a sibling pane in the current repository context, preserving the current working directory
  and user focus with HERDR's normal `pane split --current ... --cwd "$PWD" --no-focus` pattern.
- Tell the agent that the assignment is read-only and that it must not modify files, create
  branches, or spawn other agents.
- Send the initial investigation prompt without `--wait`.

If the request may modify files, classify it as Implementation instead of Investigation.

### Direct-dialogue planning

Use this mode only when the user explicitly says they will talk directly with the agent, such as
for detailed Issue or requirement planning.

- Do not create a Worktree by default; this is a planning conversation, not implementation.
- Start a named agent with the user's selected kind and provide the planning context in its
  initial prompt.
- Use `herdr agent attach <agent-name>` to hand the interaction to the user. Do not add
  `--takeover` automatically.
- If attachment fails because another client is connected, report the conflict and wait for the
  user's explicit instruction before using `--takeover`.
- Do not use `focus` as a substitute for an explicit direct handoff. `focus` only changes the
  selected HERDR target; `attach` directly connects the user to the agent terminal.

After the handoff, the supervisor does not participate in the conversation or monitor it.

## Start and detach

Use HERDR's returned IDs and names for every subsequent command. The normal background sequence
is:

```bash
herdr pane split --current --direction right --cwd "$PWD" --no-focus
herdr agent start <short-name> --kind <user-selected-kind> --pane <returned-pane-id>
herdr agent prompt <short-name> "<complete initial task>"
```

Choose `down` instead of `right` when the current layout requires it. `agent start` may perform
its one-time startup readiness handshake. This is setup, not ongoing supervision. The initial
`agent prompt` must omit `--wait`, `--until`, and completion polling.

For an implementation Worktree, use the pane/workspace returned by HERDR's Worktree operation
instead of the current pane. If an existing Worktree is requested, open it through HERDR and use
the returned live identifiers.

The initial prompt should be self-contained and should state the agent's role, task, scope,
constraints, expected artifact, and whether the work is read-only. Do not request heartbeats,
periodic updates, or an immediate status report.

After the initial prompt is accepted, stop. Do not inspect, wait for, read, focus, attach to,
send keys to, re-prompt, retry, stop, or clean up that agent or its Worktree. In particular, do
not run any of these as an automatic follow-up:

```text
herdr agent list/get/read/wait
herdr pane read/wait-output
herdr agent send-keys/focus/attach
sleep-based polling or background watchers
```

The direct-dialogue `attach` operation is the explicit handoff exception described above. A
startup or submission error may be reported, but do not blindly retry because an error or timeout
does not prove that no input was delivered.

Report only dispatch facts: the agent name, selected kind, mode, Worktree/workspace if one was
created, and that automatic monitoring is intentionally disabled. Do not claim that the delegated
work is complete.

## Explicit follow-up only

The user may later refer to an agent by its assigned name. Treat each such request as a new,
targeted operation:

- status, progress, or result: inspect only the named agent with HERDR;
- a new task or correction: send a new prompt to that named agent;
- direct interaction: attach to the named agent, using `--takeover` only when explicitly asked;
- interruption or cleanup: use the relevant HERDR command only after the user requests it;
- Worktree removal: use `herdr worktree remove` for the exact HERDR workspace, without `--force`
  unless the user explicitly requests force removal.

Do not infer a follow-up request from elapsed time, an idle pane, a completed badge, a blocked
state, or the supervisor's own expectation that the work should continue. If a follow-up does not
identify one agent and several named agents exist, ask the user which name to target.

Never invoke the repository's `workflow:worktree` skill or `git-operations:git-worktrees` skill
for a task handled under this skill. HERDR is the sole Worktree control surface here.
