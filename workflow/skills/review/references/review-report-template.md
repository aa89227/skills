# Review Report Template

The reviewer writes this report directly to the `report_path` returned by
`workflow-review get-task`. The report is an ephemeral artifact for the current review run.

```markdown
# Review Report

- Review ID: `<review-id>`
- Task ID: `<task-id>`
- Rule ID: `<rule-id>`
- Reviewed target: `<commit-or-working-tree>`
- Result: `PASS` | `FAIL` | `BLOCKED`

## Summary

<One short paragraph describing the result.>

## Steps

- `identify-change-surface`: `completed`
- `check-boundary`: `completed`
  - `inspect-dependent-boundary`: `completed` — <why this dynamic step was needed>
- `write-report`: `completed`

## Findings

### Blocking

- None

### Non-blocking

- None

### Informational

- None

## Evidence

- `<file or command and the relevant observation>`

## Limitations

- None
```

Every finding must identify evidence and its classification. A report with no findings must say
`None` in each finding section. A blocked or failed task must explain why the reviewer could not
complete the rule.
