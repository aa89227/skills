---
name: review
description: |
  Set up or run an independent, project-specific reviewer workflow. Use when the user asks to
  review an implementation, run reviewer agents, configure review rules, or prepare a result for
  GitHub workflow review. Review rules belong to the project; this skill provides orchestration,
  ephemeral external artifacts, and optional HERDR dispatch, not generic project architecture.
license: MIT
metadata:
  author: aa89227
  version: "1.1"
  tags: ["workflow", "review", "reviewer", "subagent", "herdr"]
---

# Project-specific review

## Purpose and boundary

This skill runs an independent review of the current repository state using rules owned by the
target project. It is a pre-review quality gate, not human GitHub approval and not a replacement for
the repository's own architecture, security, testing, or product decisions.

Use this gate for work that produces a repository artifact. A `Research`/`Prototype` item with no
repository artifact does not require an internal review run; the GitHub workflow's discovery
acceptance remains the applicable gate unless the user explicitly requests an internal review.

The project owns the review rules. The workflow plugin owns only the run protocol:

- project rule location and readable rule identifiers;
- one review run with a random `review_id`;
- one task per selected rule;
- external, short-lived task artifacts;
- task completion and wait semantics;
- native subagent or HERDR dispatch;
- aggregation and the blocking gate.

Do not invent generic project rules when the project has no reviewer configuration. Stop and report
the setup requirement instead. A user may explicitly enter setup mode to inspect the project and
create its first rules.

## Modes

### Setup

Enter setup mode only when the user asks to configure reviewers, or when the user explicitly agrees
to set up the missing project review mechanism. Inspect the current source, tests, configuration,
and repository conventions, then create or revise rules under:

```text
.workflow/review-rules/<rule-id>.md
```

Each rule ID is a stable, readable, project-owned identifier in lowercase kebab-case. It is not a
random run ID and must not be a class name. The file name and frontmatter `id` must match. Use the
English template in [references/review-rule-template.md](references/review-rule-template.md) for
the machine-readable metadata; write the rule explanation in the project's normal documentation
language when appropriate.

Use `.workflow/review-config.md` for the dispatch backend. The English template in
[references/review-config-template.md](references/review-config-template.md) defines `native` and
`herdr` backends. A HERDR configuration must include an explicitly selected `agent_kind`; do not
silently choose one. `required: true` means the rule is required for every normal run. Conditional
rules should be `required: false` or disabled unless the project has a deterministic applicability
mechanism.

A rule should describe only a reviewable project responsibility, such as an architectural boundary,
API compatibility promise, persistence invariant, security condition, or user-visible behavior.
It must state its scope, evidence to inspect, and what makes a finding blocking. Do not put a
specific parser class, package name, test framework, or preferred helper method in a rule unless
that detail is itself an externally maintained project constraint.

Each rule must also define an ordered review procedure. Keep the top-level steps meaningful and
review-oriented rather than listing every command. Declare which top-level steps may receive
one-level dynamic investigation children; the reviewer chooses whether to add those children from
evidence discovered during the run.

If setup discovers a new architecture, product, security, or compatibility decision that is not
already evidenced by the current source or project documentation, pause and ask whether the user
wants to discuss that decision. Do not turn an agent preference into a required rule merely because
it sounds like a best practice. Existing project constraints and decisions already visible in the
repository may be encoded without asking about every implementation choice.

Required metadata:

```yaml
type: review-rule
id: architecture-boundary
name: Architecture Boundary
enabled: true
required: true
steps: identify-change-surface,check-boundary,write-report
dynamic_parents: check-boundary
```

`steps` is an ordered, comma-separated list of top-level review steps. `dynamic_parents` is an
optional comma-separated list of top-level step IDs under which the reviewer may add investigation
steps at runtime. Dynamic steps are limited to one child level; they cannot create further child
steps. A parent step cannot complete until all of its dynamic children complete successfully.

Optional metadata may describe paths or applicability, but the primary agent must select the rules
that apply to the current change. A disabled rule is not part of the normal gate. Keep one reviewer
per rule in this version. If two independent perspectives are required, create two readable rule
IDs with separate responsibilities; do not create random duplicate IDs. Multiple reviewers for one
rule is deliberately deferred until a project demonstrates a concrete need such as independent
security opinions.

If HERDR is the project's configured dispatch backend, setup must also record the allowed reviewer
agent kind. Do not silently guess a HERDR agent kind when the installed HERDR instructions require
the user to choose one.

### Run

Before a run:

1. Read the current source, tests, configuration, and the target change. Do not use an old Issue or
   decision record as current implementation truth.
2. Load `.workflow/review-rules/*.md`, validate the metadata, and select enabled rules applicable
   to the current change. If the directory is absent, empty, or invalid, block the review.
3. Start one run with the stable `workflow-review` command and one `--rule-id` for every selected
   rule. The command returns a random `review_id` and an artifact directory outside the project.
4. Create one independent reviewer task per selected rule. The reviewer must call `get-task` before
   reviewing so it receives the rule snapshot, project root, task ID, and report path.

The reference shell engine is at `scripts/workflow-review`. The repository package exports it as the
stable `workflow-review` command; Claude/Codex host setup must register the same stable launcher
when its plugin installer does not install package binaries. Never invoke a versioned plugin-cache
path in the normal workflow; a versioned absolute path can cause needless host approval churn. A
stable command still remains subject to the host's shell approval and sandbox policy; this skill
cannot bypass those policies. The reference engine requires Bash 4+, `jq`, `flock`, `mktemp`,
`cksum`, and either `sha256sum` or `shasum`, plus common Unix utilities such as `awk`, `sed`,
`cp`, `mv`, `mkdir`, and `rm`. Setup must report a missing dependency instead of silently
switching to a different protocol.

The engine snapshots the current Git `HEAD`, tracked diff, and untracked file contents when a run
starts. It invalidates the run if that snapshot changes, including when a file was already dirty
before the review began. Non-Git roots use a recursive file snapshot instead.

Use this protocol:

```bash
workflow-review start \
  --project-root "$PWD" \
  --rule-id architecture-boundary \
  --rule-id api-compatibility

workflow-review get-task \
  --review-id "$REVIEW_ID" \
  --rule-id architecture-boundary

workflow-review start-step --task-id "$TASK_ID" --step-id identify-change-surface
workflow-review complete-step --task-id "$TASK_ID" --step-id identify-change-surface

workflow-review start-step --task-id "$TASK_ID" --step-id check-boundary
workflow-review add-step \
  --task-id "$TASK_ID" \
  --parent-step-id check-boundary \
  --step-id inspect-dependent-boundary \
  --reason "A new dependency crosses the reviewed boundary."
workflow-review start-step --task-id "$TASK_ID" --step-id inspect-dependent-boundary
workflow-review complete-step --task-id "$TASK_ID" --step-id inspect-dependent-boundary
workflow-review complete-step --task-id "$TASK_ID" --step-id check-boundary

workflow-review status --review-id "$REVIEW_ID"
workflow-review submit --task-id "$TASK_ID" --status completed
workflow-review wait --review-id "$REVIEW_ID"
workflow-review cleanup --review-id "$REVIEW_ID"
```

The primary agent must retain the returned `review_id`, task IDs, and artifact directory in the
current session only. The run directory is ephemeral and lives under `/tmp/workflow-reviews/`; do
not commit it, copy it into the project, attach it to a GitHub Issue, or treat it as an audit log.
After reading the reports and recording the current outcome, call `cleanup`; an interrupted run may
remain only until the host's normal `/tmp` cleanup.
The primary agent reads the report files directly after `wait` and may summarize the result in the
current response or current PR workflow. If the process resumes without the run artifacts, start a
new review instead of reconstructing an old result.

## Reviewer task contract

The reviewer is independent from the primary implementation agent and is read-only with respect to
the project. The reviewer:

1. receives only the review ID and its assigned stable rule ID in the initial dispatch prompt;
2. calls `workflow-review get-task --review-id <id> --rule-id <rule-id>`;
3. calls `start-step` and `complete-step` for every configured top-level step in order;
4. adds dynamic child steps only under an active parent listed by `dynamic_parents`, then runs and
   completes each child before completing its parent;
5. reads the current project state and the returned rule content;
6. writes step artifacts directly inside the returned external task directory and writes its report
   directly to the returned `report_path` inside the returned external task directory; it may add
   supporting files in that directory;
7. does not modify project source, tests, configuration, Issues, PRs, branches, commits, or
   worktrees;
8. calls `workflow-review submit --task-id <task-id> --status completed` after all steps and the
   report are complete,
   or `--status failed` / `--status blocked` when it cannot complete the assignment.

`get-task` is a one-time claim for a rule in a review run. Dispatch exactly one reviewer for each
task. A repeated claim is an orchestration error; use the already returned task ID for a retry by
the same reviewer rather than starting another reviewer for that rule.

`start-step`, `add-step`, `complete-step`, and `submit` record state only. They must never receive
report text and must never be used as file writers. Use
[references/review-rule-template.md](references/review-rule-template.md) for the step metadata and
[references/review-report-template.md](references/review-report-template.md) for the report shape.
Every finding is classified as `Blocking`, `Non-blocking`, or `Informational`; a reviewer's
preference is not automatically a blocking finding.

The primary agent may call `workflow-review status --review-id <id>` while tasks are running. The
status output exposes step state and dynamic-step relationships, but the primary agent must not read
or edit a reviewer's in-progress report or step artifacts. The reviewer does not need approval for a
dynamic child step; the rule's declared `dynamic_parents` and the one-level limit are the boundary.

The primary agent must not read another reviewer's task directory while that reviewer is working,
must not edit a reviewer's report to change its conclusion, and must not silently fix code during
aggregation. Fixes are primary-agent implementation work and require a new review run after the
current code changes.

## Dispatch backends

- Outside HERDR, use the host's native subagent mechanism when it is available.
- When `HERDR_ENV=1` and `.workflow/review-config.md` sets `backend: herdr`, use
  `workflow:herdr-supervisor` as the dispatch backend after this review skill was explicitly
  invoked. Dispatch one investigation
  task per rule and use the review artifact protocol as the return channel. Do not wait for or read
  HERDR pane output; `workflow-review wait` is the synchronization point.
- If HERDR is unavailable, do not manually imitate HERDR commands. Use native subagents only when
  the host provides them; otherwise block and report the missing dispatch capability.

The reviewer backend must not create a project worktree for a read-only review unless the project
rule explicitly requires an isolated checkout. A review task is not an implementation task.

The review target is the repository state captured at `start`, including whether the working tree
was dirty. Reviewers must not treat a later source change as part of the same run; the engine
invalidates a run when it detects a target change while waiting.

## Completion gate

The review passes only when all selected required rules have terminal `completed` tasks, every
required task has completed all configured and dynamic steps, every required report is present, and
no report contains an unresolved `Blocking` finding. Optional-rule failures and non-blocking
findings must be reported, but do not silently disappear.

`workflow-review wait` means only that task states are terminal; it is not itself a passing review.
The primary agent must read the reports, classify findings, and apply the gate above before
reporting `PASS`.

If a required task is missing, times out, fails, or is blocked, the review is blocked. Do not mark a
GitHub Issue or PR as ready for human review on the basis of a partial run. A passing internal
review does not create a GitHub `APPROVED` review and does not authorize merge. The human approval
rules in `workflow:github-project-workflow` remain authoritative.

When the GitHub workflow is used, the primary agent must complete this gate before moving an Issue
from `In Progress` to `In Review`. The internal result is evidence for that transition only during
the current run; it does not replace persisted PR review history, and it is not required again merely
because an Issue already in `In Review` is later inspected. If implementation changes after the
run, return to `In Progress` and start a new run before re-entering `In Review`.

## Final response

Report, in the user's language:

- the random review ID and target commit or working-tree description;
- selected rule IDs and task states;
- the external artifact directory for the current run;
- blocking, non-blocking, and informational findings;
- whether the internal gate passed, failed, or was blocked;
- any separate human GitHub approval still required.

Do not claim that the review is complete when `wait` timed out, a required task failed, or a report
is missing. Do not preserve the ephemeral report as a repository decision record unless the user
explicitly asks for a separate durable decision or requirement update.
