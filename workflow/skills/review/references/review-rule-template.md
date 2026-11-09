# Project Review Rule Template

Copy this file to `.workflow/review-rules/<rule-id>.md` and replace the placeholders. Keep the
identifier stable and readable. The frontmatter is intentionally small so the workflow engine can
validate it without knowing the project's implementation language.

```markdown
---
type: review-rule
id: architecture-boundary
name: Architecture Boundary
description: Protects the project's approved dependency direction and public boundaries.
enabled: true
required: true
paths:
  - src/**
---

# Architecture Boundary

## Scope

Review changes under `src/` for violations of the approved dependency direction.

## Evidence to inspect

- The changed files and their direct dependencies.
- The project's current architecture documentation and tests.
- Any public behavior changed by the implementation.

## Review questions

- Does the change preserve the approved boundary?
- Does it introduce a dependency or side effect that the project has not approved?
- Are observable behavior and compatibility preserved where required?

## Blocking findings

Classify a finding as `Blocking` only when the change violates the boundary, breaks a required
contract, or makes the intended behavior unsafe or unverifiable. Classify preferences and future
improvements as `Non-blocking` or `Informational`.
```
