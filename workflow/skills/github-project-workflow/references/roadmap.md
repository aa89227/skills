# GitHub Project Roadmap

Use this reference when planning short-, medium-, and long-term product work or when creating and
maintaining a Project roadmap view. A roadmap is a planning view, not a lifecycle status, approval
gate, requirement specification, or published release record.

GitHub Projects provides a roadmap layout that displays issues, pull requests, and draft issues on
a timeline using date or iteration fields. It can also show milestones as vertical markers. Use the
native Project roadmap view rather than inventing a `Roadmap` lifecycle status.

## Roadmap fields

Use existing Project fields when available. If the Project is being explicitly set up for this
workflow, create or configure these non-lifecycle fields:

| Field | Values/format | Meaning |
| --- | --- | --- |
| `Roadmap Horizon` | `Short`, `Medium`, `Long` | Planning horizon; never a lifecycle status |
| `Start Date` | Project date | Known or Human-selected start date |
| `Target Date` | Project date | Known or Human-selected target date |
| `Roadmap Confidence` | `Committed`, `Planned`, `Exploratory` | Confidence in the plan; not a promise of completion |
| `Next Milestone` | Exact milestone title or `None` | Explicit rollover destination for the current release plan |

The agent MUST NOT invent dates or versions. Leave dates unset when unknown. A Roadmap Horizon or
Roadmap Confidence value MUST NOT move an Issue through lifecycle statuses or authorize coding,
review, merge, or release.

## Planning horizons

Use a rolling planning window:

- `Short`: the repository's current release or an explicitly configured `Next Milestone`, with a
  human-approved committed Requirement or Initiative child set.
- `Medium`: an exact future Milestone supplied by Human or repository release metadata, but not the
  current or next release. These are planned items; no Issue is release-eligible until it is `Done`
  and present in the matching tag.
- `Long`: no exact Milestone, an unresolved Initiative, or a Research/Prototype item. Use the
  Roadmap view and dates only when Human has supplied them; do not assign an exact release
  Milestone until the version is decided.

Do not create a formal Milestone named `Long-term`, `Future`, or another vague horizon. Use
`Roadmap Horizon` and `Roadmap Confidence` for that planning information. Formal release Milestones
MUST use the repository's exact version convention, currently `v<major>.<minor>.<patch>`.

## Automatic roadmap placement

When a configured Roadmap view and its fields are available, the agent MUST place a resulting
Initiative or Requirement without asking the user to learn the roadmap model:

1. Put a human-approved deliverable for the current release or explicit `Next Milestone` in
   `Short`, set `Roadmap Confidence: Committed`, and use that exact Milestone.
2. Put a human-supplied future release deliverable in `Medium`, set `Roadmap Confidence: Planned`,
   and use that exact future Milestone.
3. Put an unresolved Initiative or a Research/Prototype item in `Long`, set `Roadmap Confidence:
   Exploratory`, and leave Milestone as `None` unless a concrete release has been approved.
4. Use `Start Date` and `Target Date` only when provided or already defined by Project policy.
5. Keep child Requirements visible in the roadmap; group or filter by parent, horizon, milestone,
   or confidence instead of duplicating their descriptions in the parent.

If a request later becomes concrete, update the Roadmap fields and assign the exact Milestone. This
is planning metadata and does not replace the Issue's specification or human approval gate.

## Initiative and roadmap relationship

An Initiative MAY appear as a parent item in the roadmap, with its child Requirements beneath it.
The parent provides product-level context; the child Issue provides the actionable requirement and
the PR relationship. A single ordinary Requirement MAY appear directly in the roadmap without an
Initiative. Not every Issue needs a parent Initiative.

The roadmap can show parent/sub-issue progress using GitHub's native hierarchy. The agent MUST NOT
create a parent solely to make the roadmap look organized, and MUST NOT split a single logical
requirement merely because it spans a long timeline.

## Setup and mutation boundary

Creating a Project roadmap view or non-lifecycle custom fields is a Project setup mutation. The
agent MAY do it only when the user explicitly requests Project setup or repository policy grants
that authority. Otherwise, use existing fields and report the exact missing setup. If planning
metadata cannot be placed in the Project, the agent MAY record the same values in the relevant
Issue's `Workflow Metadata` and MUST state that the Project roadmap is not synchronized. Never
create a new lifecycle status to compensate for a missing roadmap field.
