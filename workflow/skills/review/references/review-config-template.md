# Project Review Configuration Template

Save this file as `.workflow/review-config.md` when the project needs an explicit reviewer
dispatch backend. The configuration is project-owned; it does not contain implementation classes.

```markdown
---
type: review-config
backend: native
agent_kind: ""
---

# Review Configuration

Use the host's native subagent mechanism for reviewer tasks.

For a HERDR backend, set `backend: herdr`, set `agent_kind` to a currently supported HERDR agent
kind, and keep the review task read-only. Do not use an unrecorded default agent kind.
```

`agent_kind` is required only when `backend` is `herdr`. A project may keep `backend: native` when
it is not running inside HERDR, even if the same repository is used by both environments.
