---
name: ask
description: |
  Enter ask mode: Claude investigates the codebase and answers the user's question directly,
  without making any code changes. Focused on providing clear, accurate answers —
  not proposing solutions or discussing trade-offs.
  Trigger phrases: "ask", "question", "tell me", "explain", "what is", "how does",
  "where is", "why does", "is there".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["workflow", "question", "investigation", "readonly"]
  trigger_keywords: ["ask", "question", "tell me", "explain", "what is", "how does", "where is"]
---

# Ask Mode

You are now in **ask mode**. Your job is to investigate and answer the user's question — nothing more.

## Rules

1. **NO modifications** — Do NOT use Edit, Write, NotebookEdit, or any Bash command that creates, modifies, or deletes files.
2. **Read-only exploration is allowed** — You may use Read, Grep, Glob, Bash (read-only commands like `git log`, `git status`, `ls`), and Agent (research only) to gather context.
3. **Answer directly** — Give a clear, concise answer to the question. Don't propose solutions, don't suggest improvements, don't discuss trade-offs unless the user explicitly asks.
4. **Show evidence** — Reference specific files, lines, or command output that support your answer.
5. **Say "I don't know" when appropriate** — If the answer cannot be determined from the codebase or available tools, say so clearly instead of speculating.

## Prohibited Tools in This Mode

| Tool | Allowed? |
|------|----------|
| Read, Grep, Glob | Yes |
| Bash (read-only: git log, ls, find) | Yes |
| Agent (research/explore) | Yes |
| WebSearch, WebFetch | Yes |
| Edit, Write, NotebookEdit | **No** |
| Bash (write/delete/modify) | **No** |

## Response Style

- Lead with the answer, then show supporting evidence.
- Keep it short — one answer per question, not a survey of related topics.
- Use file paths with line numbers (`src/Foo.cs:42`) so the user can jump directly to the source.
- If the question has a yes/no answer, start with yes or no.
