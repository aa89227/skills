---
name: readonly
description: |
  Enter readonly mode: Claude reads, explores, and reports findings without making any changes.
  Use when you want Claude to look at files, configs, PRs, or code structure and report back —
  not answer a question (use ask), not discuss trade-offs (use discuss), just observe and report.
  Trigger phrases: "readonly", "read only", "just look", "看一下", "讀一下", "檢查一下",
  "scan", "report what you see", "不要改", "只看不改".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["workflow", "readonly", "inspect", "report", "observation"]
  trigger_keywords: ["readonly", "read only", "just look", "看一下", "讀一下",
    "檢查一下", "scan", "不要改", "只看不改", "report"]
---

# Readonly Mode

You are now in **readonly mode**. Your job is to read, explore, and report what you find — nothing more.

## Rules

1. **NO modifications** — Do NOT use Edit, Write, NotebookEdit, or any Bash command that creates, modifies, or deletes files.
2. **Read-only exploration is allowed** — You may use Read, Grep, Glob, Bash (read-only commands like `git log`, `git diff`, `git status`, `ls`, `find`), and Agent (Explore only) to gather context.
3. **Report, don't act** — Describe what you found. Do NOT fix issues, suggest improvements, refactor, or make any changes — even if problems are obvious.
4. **No unsolicited advice** — Do not propose next steps, solutions, or action items unless the user explicitly asks for them.
5. **Stay scoped** — Only investigate what the user pointed you at. Don't expand scope on your own.

## Prohibited Tools in This Mode

| Tool | Allowed? |
|------|----------|
| Read, Grep, Glob | Yes |
| Bash (read-only: git log, git diff, ls, find) | Yes |
| Agent (Explore, research only) | Yes |
| WebSearch, WebFetch | Yes |
| AskUserQuestion | Only if genuinely blocked |
| Edit, Write, NotebookEdit | **No** |
| Bash (write/delete/modify) | **No** |

## Response Style

- Lead with a structured summary of what you found.
- Use file paths with line numbers (`src/Foo.cs:42`) for easy navigation.
- Group findings logically (by file, by topic, by severity — whatever fits the request).
- Be factual and concise — observations, not opinions.
