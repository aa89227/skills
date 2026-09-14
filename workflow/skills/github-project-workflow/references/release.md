# Milestone and Release Specification

Use this reference when planning a release, evaluating milestone contents, or generating and
publishing user-facing release notes.

## Planning

A GitHub Milestone is the release-planning source of truth. It records the version an Issue is
planned for, such as `v1.4.0`; it does not prove delivery. Stable releases MUST use a Git tag in
the exact form `v<major>.<minor>.<patch>`, such as `v1.4.0`. The agent MUST NOT guess a release
version. An Issue MAY have no milestone (`None`). Milestone and lifecycle status are independent.

The release tag identifies the exact release target commit. The GitHub Release created for that tag
remains the published-version source of truth.

Only an Issue with `Status = Done` and verified content in the commit targeted by the release tag
is eligible for that release. `Milestone = v1.4.0` alone is insufficient. A Done Issue whose
changes are absent from the tag target commit MUST be excluded and investigated.

Before a release, incomplete milestone Issues MUST be moved to the explicitly configured `Next
Milestone` or otherwise replanned. The agent MAY perform that move only when the next milestone is
explicitly known in Project or repository release metadata; it MUST ask or report the unresolved
decision rather than invent a version.

## Release-note source and categories

Generate user-facing release notes from completed, release-eligible Issues:

```text
Milestone Issues
→ select Status = Done
→ verify content in the release tag target commit
→ read Type and Issue Release Note
→ exclude Release Note = None
→ categorize
→ generate notes
```

Default categories are:

- `New Features`: `Feature`
- `Improvements`: `Improvement`
- `Bug Fixes`: `Bug`
- `Documentation`: `Documentation`, only when user-facing documentation is worth publishing

Do not normally publish `Refactor`, `Chore`, or other internal changes to general users. Do not
derive user-facing wording directly from commit history. A `Bug` with a non-`None` Issue Release
Note SHOULD appear under `Bug Fixes`; an internal refactor with `Release Note: None` MUST NOT appear
in user-facing notes.

## Release verification

Before creating a GitHub Release, the agent MUST:

1. Inspect all Issues in the milestone.
2. Verify each included Issue is `Done`.
3. Verify each included Issue's actual result exists in the commit targeted by the release tag.
4. Replan or move incomplete Issues when the destination milestone is explicitly known; otherwise
   report the decision required.
5. Verify every included Issue's Type and Release Note.
6. Verify the `v<major>.<minor>.<patch>` tag against repository policy and the human-provided
   release version.
7. Generate categorized notes from the eligible Issues.
8. Create the GitHub Release for the verified tag.
9. Confirm the published tag, version, and notes, then close the Milestone.

The GitHub Release is the published-version source of truth. Closing a Milestone before a verified
GitHub Release is published is invalid. A release failure leaves planning metadata open for
reconciliation; it does not justify changing eligible Issues back to another lifecycle status.
