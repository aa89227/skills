# Initiative Decomposition and Discovery

Use this reference when a user request may contain several independently deliverable outcomes,
or when high-impact unknowns require discussion, research, or a prototype before the requirement
can be specified. The user does not need to request or manage an Epic; the agent decides whether an
initiative container is warranted and keeps the hierarchy recoverable in GitHub.

## Planning model

Use GitHub's native parent Issue and sub-issue relationship as the hierarchy source of truth. Do not
simulate hierarchy with labels, title prefixes, comments, or a manually maintained numbered list.
GitHub supports parent/sub-issue progress in Projects, so the relationship can be viewed without
duplicating it in custom metadata.

An Initiative MAY be created only when the native parent/sub-issue relationship can be created and
verified. If the repository or permissions do not provide that relationship, the agent MUST NOT
pretend that labels or links are an equivalent hierarchy; keep the request in `Specifying` and
report the exact setup or permission blocker.

There are two Issue roles:

- `Requirement`: one logical, independently verifiable requirement. It follows the ordinary
  lifecycle and may have implementation PRs.
- `Initiative`: a planning parent for a large or uncertain product outcome. It organizes child
  Requirements, has no implementation PR of its own, and is not independently release-eligible.

The existing Issue Type vocabulary remains unchanged. `Issue Role: Initiative` is workflow metadata;
an organization MAY display a custom `Epic` Issue Type, but the agent MUST NOT depend on that type
for hierarchy or lifecycle behavior.

## Decide whether to decompose

Create one ordinary `Requirement` Issue when all of these are true:

- the request has one primary user-visible outcome;
- scope and out-of-scope can be stated without inventing multiple deliverables;
- acceptance criteria can be written now or after ordinary clarification; and
- no high-impact unknown requires a separate research or prototype result first.

Create an `Initiative` parent with child Issues when at least one of these objective triggers is
present:

1. The request contains two or more independently verifiable or independently releasable user
   outcomes.
2. A high-impact unknown affects expected behavior, scope, an important constraint, or the choice
   between viable solutions, and it requires research or a prototype.
3. The request spans separate product areas or release slices that can be accepted independently.

Implementation size, file count, expected commit count, or the need for multiple PRs is not enough
to trigger decomposition. A large but single-outcome implementation remains one Requirement Issue.
If the request is initially unclear but does not meet a trigger, start one Issue in `Inbox` or
`Specifying`; promote it to an Initiative before implementation when discovery reveals a trigger.
Promotion after implementation has started requires a material requirement decision and explicit
human direction.

When one Requirement contains both a new or changed Design System contract and its runtime
implementation, apply [design-system-handoff.md](design-system-handoff.md): complete `design`
before `build`, regardless of the Issue's `Ready` status. If the design handoff and implementation
are independently verifiable or the combined request is too large to review coherently, recommend
a native design child Issue and implementation child Issue. Size alone does not force a split, and
the recommendation MUST NOT introduce a `Designing` or other custom lifecycle Status.

Before creating the parent, the agent MUST outline at least two meaningful child work items. If
fewer than two exist, create one ordinary Requirement Issue and do not create a parent. If an
existing Initiative later collapses below two children, the agent MUST NOT delete or silently
convert Issues; keep the parent in `Specifying` and report the required Human decision.

## Automatic decomposition procedure

When a trigger is present, the agent MUST:

1. Create or retain one parent `Initiative` Issue using the user's selected language. Its body MUST
   describe the product outcome, background, scope, out-of-scope, overall success conditions,
   discovery questions, and a concise child map. Its `Release Note` MUST be `None`; child
   Requirements carry any user-facing release notes, and the parent is never independently
   release-eligible.
   Its Workflow Metadata MUST set `Issue Role: Initiative`, `Parent Issue: None`, and
   `Approval Source: None`; use `Work Mode: Discussion` while the decomposition is being
   specified.
   The parent MUST use an existing Issue Type that describes the aggregate outcome: `Feature` for
   a new capability, `Improvement` for a change to an existing capability, `Bug` for defect
   remediation, and `Documentation`, `Refactor`, or `Chore` only when that category describes all
   child work. For a mixed initiative, use `Feature` when it includes a new capability; otherwise
   use `Improvement`.
   Set the parent Milestone to an exact planned release only when Human has assigned that same
   release to every required Delivery child; otherwise use `None`. Child Milestones remain the
   release-planning record for each independently deliverable result.
2. Create one child Issue for each independently verifiable product outcome or bounded discovery
   result. Set the native parent/sub-issue relationship immediately.
3. Give every child the normal Issue template, its own acceptance criteria, Type, Release Note,
   language metadata, and `Parent Issue` metadata. Do not copy the entire parent body into every
   child.
4. Assign each child one `Work Mode` from the closed set below. `Work Mode` is metadata, not a
   lifecycle status. A bounded production Requirement MUST use `Delivery`; use `Discussion`,
   `Research`, or `Prototype` only when that child is a discovery item.
5. Keep the parent and children in planning states until the decomposition is human-approved. Do
   not start implementation while the plan or a child specification is still being clarified.

## Work Modes

Use `Work Mode` in Project metadata when the field exists; otherwise record it in each Issue's
`Workflow Metadata`.

| Work Mode | Use when | Required result |
| --- | --- | --- |
| `Discussion` | The next action is to clarify a product decision or unresolved requirement. | A resolved decision or an updated canonical specification. No implementation. |
| `Research` | A bounded question needs evidence before behavior or design can be approved. | Evidence, a decision, or a constraint recorded in the child and parent Issue. Default Release Note is `None`. |
| `Prototype` | A time-boxed experiment is needed to reduce uncertainty about feasibility or user behavior. | Findings and a go/no-go decision recorded in the child and parent Issue. Prototype output is not production behavior by default. |
| `Delivery` | The child is an approved production requirement. | The user-visible or operational result, verified and integrated through the normal PR workflow. |

`Task` is not a Work Mode. Internal coding steps belong in a child Issue's checklist, PR, or
commits. Create a child Issue only when the work is independently verifiable, reviewable, a
blocking dependency, a research result, or a prototype result.

`Discussion` is a temporary clarification mode for a Requirement or discovery child. Such a child
MUST remain in `Inbox` or `Specifying`; it MUST NOT enter implementation. When the discussion
resolves, the agent MUST update the canonical body and set the child mode to `Delivery`, `Research`,
or `Prototype`, then obtain the applicable specification approval before proceeding. A Discussion
child MUST NOT be treated as a releasable or independently completable delivery item. An Initiative
parent MAY retain `Work Mode: Discussion` while its aggregate lifecycle advances; the parent is
governed by the aggregate rules below, and child modes determine executable work.

## Discovery item lifecycle

Research and Prototype Issues, including Initiative children, still use only the eight formal
lifecycle statuses. They MUST NOT create `Discussing`, `Researching`, `Prototyping`, `Task`, or
`Prototype` statuses.

- `Inbox` → `Specifying`: define the bounded question or experiment.
- `Specifying`: record the question, scope, time box, evidence needed, and decision criteria.
- `Ready`: a human-approved discovery plan; parent aggregate approval MAY satisfy this gate when
  the child body was included in the approved parent decomposition.
- `In Progress`: conduct the research or experiment; update findings in the Issue body.
- `In Review`: the evidence and conclusion are ready for human review.
- `Ready to Merge`: human accepts the discovery result. If the result has a repository artifact,
  use a PR and its selected Review Mode; if it has no artifact, the human may directly set the
  child Project Status to `Ready to Merge` after reviewing the recorded result.
- `Done`: the accepted findings are recorded in the child and parent, and any repository artifact
  has been integrated into its explicitly declared target. A discovery item is not release-eligible
  merely because it is `Done`.

If a Prototype becomes a production requirement, change its `Work Mode` to `Delivery`, update the
Issue body, and return to `Specifying` for a new specification approval. Do not silently ship a
prototype because its experiment succeeded. If research or a prototype is rejected, Human may
cancel it; the agent MUST NOT cancel it merely because the result is negative.

## Aggregate parent lifecycle

The parent Initiative uses the same formal statuses, but its criteria are aggregate:

| Parent status | Meaning |
| --- | --- |
| `Inbox` | The broad outcome has been captured but decomposition has not started. |
| `Specifying` | The agent is clarifying the outcome, creating child Issues, and resolving discovery questions. |
| `Ready` | Human approved the parent scope and current child decomposition. This does not authorize implementation of a child whose body changed afterward. |
| `In Progress` | At least one child is actively researching, prototyping, or delivering; the parent remains here while required children remain unfinished. |
| `In Review` | Every required child is reviewable, `Ready to Merge`, or `Done`; the parent-level outcome and roadmap placement can be reviewed. |
| `Ready to Merge` | Every required Delivery child has valid approval under its selected Review Mode or is `Done`, every discovery child has an accepted result, and the aggregate outcome is ready for integration/closure. |
| `Done` | Every required child is `Done`, all required delivery results are in the remote target, discovery results are durable, and the parent scope is complete. |
| `Cancelled` | Human cancelled the initiative. Child Issues are not auto-cancelled; Human must decide whether each child is cancelled, reparented, or retained. |

The parent MUST use only transitions allowed by [lifecycle.md](lifecycle.md). A child entering
implementation feedback does not by itself reset the parent; a child material requirement change
resets the parent to `Specifying` only when it changes the approved parent scope, child map, or
aggregate outcome. A child that is removed from scope requires an updated parent body and new parent
specification approval.

Adding a child, removing a child, or changing the child map after the parent is `Ready` is a
material parent specification change. The parent MUST return to `Specifying`, update its body, and
obtain new parent approval before the changed decomposition is implemented. If a child is
`Cancelled`, it does not count as complete; Human MUST decide whether to replace, reparent, remove,
or retain it, and the parent cannot become `Done` until the approved child plan is complete.

## Aggregate approval and handoff

When Human changes the parent Project Status from `Specifying` to `Ready`, that action MAY approve
the current parent body and all linked child bodies shown in the child map at that moment. The agent
MAY then move unchanged child Requirements from `Specifying` to `Ready` and MUST record
`Approval Source: parent #<number>` in their Workflow Metadata. A child created or materially
changed afterward requires its own human Project Status change to `Ready`.

Every child Requirement remains the source of truth for its own acceptance criteria and PRs. The
parent is a planning and aggregate-progress record, not a replacement for child specifications.
