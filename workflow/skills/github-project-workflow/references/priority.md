# Issue Priority

Use this reference when assigning or reassessing the action order of a GitHub Issue. Priority is
the current decision about how soon the Issue should receive attention. It is not a technical
severity score, a security ranking, a lifecycle Status, a review result, or a roadmap horizon.

## Values

Every Issue has one of these values. `Untriaged` is the default when the current artifacts do not
provide enough evidence for a decision.

| Value | Meaning | Required basis |
| --- | --- | --- |
| `Untriaged` | The Issue has not been sufficiently assessed for action order. | Do not guess; record that the decision is pending or evidence is missing. |
| `P0` | Immediate action is required because an active major impact exists or a named current merge, release, or lifecycle gate is at imminent risk. | Name the active impact or exact gate/release and cite reproducible evidence, a failed criterion, or an established incident. |
| `P1` | The Issue is committed for a specific delivery point but does not currently require P0 treatment. | Use an existing or Human-supplied exact GitHub Milestone or Target Date. The agent MUST NOT invent one. |
| `P2` | The Issue is valid and worth keeping, but has no current exact commitment or deadline. | State the value or impact, and why no P0/P1 condition currently applies. |

Do not create additional Priority values unless the workflow is deliberately revised. A security
label, a theoretical vulnerability, a reviewer concern, or an old Issue is not by itself a basis
for `P0`.

## Assignment rules

- Set `Untriaged` when the Issue lacks enough current facts to establish impact, commitment, or
  timing.
- Set `P0` only when the Issue must be handled ahead of ordinary sequencing. A routine review
  finding that prevents approval is not automatically `P0`.
- Set `P1` only when the exact Milestone or Target Date already exists in GitHub or was explicitly
  supplied by Human or repository policy. A phrase such as “next sprint” or “soon” is insufficient.
- Set `P2` when the requirement is useful but is not currently committed to a named delivery point.
- Priority does not authorize coding, review approval, merge, release, or a lifecycle transition.
  Follow the ordinary lifecycle and approval rules independently.

The agent MAY recommend a Priority, but it MUST NOT turn a recommendation into a Human commitment.
When a Project Priority field is unavailable, record the value and rationale in the Issue's
`Workflow Metadata`; do not create a Project field unless Project setup is explicitly authorized.

For an `Initiative`, each child Requirement has its own Priority. The parent Priority describes the
urgency of the aggregate outcome and MUST NOT be raised merely because one child has a higher
Priority; use the parent-level impact, commitment, or gate evidence.

## Rationale and evidence

Whenever Priority is assigned or changed, update the canonical Issue body with one concise
`Priority Rationale`. It MUST contain both the reason and the evidence. Comments MAY preserve the
change history, but the current value and rationale MUST not exist only in comments.

```text
Priority: P1
Priority Rationale: Committed to Milestone `v1.4.0`; the Issue is needed for the approved export flow, and the current workaround is not acceptable for that release.
```

For `Untriaged`, use `Priority Rationale: None` only when no decision has been made; if evidence
is missing, state what must be learned before triage.

## Reassessment

Priority is allowed to change when a material fact changes, including:

- a new reproducible impact, incident, exposure, or affected user flow;
- a current merge, release, or lifecycle gate becomes blocked or is no longer at risk;
- a Milestone, Target Date, dependency, or Human commitment is added, removed, or changed; or
- a mitigation changes the remaining work and its urgency.

Elapsed time alone MUST NOT raise or lower Priority. An approaching or missed date is a reason to
reassess, not an automatic `P0`; use `P0` only when the resulting impact or gate risk meets the
definition above.

These are the expected evidence-based changes:

| From | To | When justified |
| --- | --- | --- |
| `Untriaged` | `P0`, `P1`, or `P2` | Triage obtains the required impact, commitment, or backlog rationale. |
| `P2` | `P1` | An exact Milestone or Target Date is explicitly committed. |
| `P2` or `P1` | `P0` | A major active impact appears, or a named current gate/release becomes imminently at risk. |
| `P0` | `P1` or `P2` | The immediate condition is mitigated; record the remaining commitment or why none exists. |
| `P1` | `P2` | The exact commitment or delivery point is removed or no longer applies. |

When changing the value, update the Project field and Issue body together when both are available,
preserve the previous value in history, and do not move lifecycle Status solely because Priority
changed. If the change also changes behavior, scope, acceptance criteria, or an important
constraint, apply the material-requirement rules in [issue.md](issue.md).
