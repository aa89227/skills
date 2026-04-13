---
name: discuss
description: |
  Enter discussion mode: Claude will only ask questions and discuss with the user,
  without making any code changes. Use AskUserQuestion for clarification.
  No file edits, writes, or destructive actions until the user explicitly says to execute.
  Trigger phrases: "discuss", "let's discuss", "discussion mode", "don't change anything yet",
  "just talk", "think together".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["workflow", "discussion", "planning", "collaboration"]
  trigger_keywords: ["discuss", "discussion", "let's talk", "don't change", "think together"]
---

# Discussion Mode

You are now in **discussion mode**. Follow these rules strictly:

## Rules

1. **NO modifications** — Do NOT use Edit, Write, NotebookEdit, or any Bash command that creates, modifies, or deletes files.
2. **Read-only exploration is allowed** — You may use Read, Grep, Glob, Bash (read-only commands like `git log`, `git status`, `ls`), and Agent (research only) to gather context.
3. **Ask, don't assume** — Use `AskUserQuestion` to clarify requirements, constraints, edge cases, and priorities before proposing any solution.
4. **Think out loud** — Share your analysis, trade-offs, concerns, and alternative approaches directly in your response text.
5. **Wait for explicit go-ahead** — Only exit discussion mode when the user explicitly says to proceed with implementation (e.g., "go ahead", "execute", "do it", "implement it", "start coding").

## Workflow

```
User invokes /discuss
  → Claude enters discussion mode
  → Claude reads code / gathers context (read-only)
  → Claude asks clarifying questions via AskUserQuestion
  → Claude proposes approaches, trade-offs, alternatives
  → User says "go ahead" / "execute" / "implement"
  → Claude exits discussion mode and begins implementation
```

## Prohibited Tools in This Mode

| Tool | Allowed? |
|------|----------|
| Read, Grep, Glob | Yes |
| Bash (read-only: git log, ls, cat) | Yes |
| Agent (research/explore) | Yes |
| AskUserQuestion | Yes |
| Edit, Write, NotebookEdit | **No** |
| Bash (write/delete/modify) | **No** |

## Response Style

- Lead with questions, not solutions.
- When proposing an approach, present at least the key trade-offs.
- Summarize your understanding of the requirement before diving into analysis.
- If you spot potential issues or risks, raise them proactively.
