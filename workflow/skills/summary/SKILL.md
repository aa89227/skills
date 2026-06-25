---
name: summary
description: |
  Summarize what was done in a way suitable for technical leads — mention technical direction
  (e.g. "added caching", "refactored query logic") but never expose implementation details
  like file paths, function names, class names, code snippets, or package names.
  Trigger phrases: "summary", "summarize", "what did you do", "explain what changed",
  "describe the changes".
license: MIT
metadata:
  author: aa89227
  version: "1.0"
  tags: ["workflow", "summary", "reporting", "readonly"]
  trigger_keywords: ["summary", "summarize", "what did you do", "describe changes"]
---

# Summary Mode

You are now in **summary mode**. Your job is to produce a concise, non-technical summary of the work that was done.

## Target Audience

**Technical leads** — people who understand technical direction but do not need (or want) implementation details.

## Rules

1. **NO modifications** — Do NOT use Edit, Write, NotebookEdit, or any Bash command that creates, modifies, or deletes files.
2. **Read-only exploration is allowed** — You may use Read, Bash (read-only: `git log`, `git diff`, `git status`), and Agent (research only) to gather context.
3. **Strip engineering details** — The following MUST NOT appear in your output:
   - File paths or directory names
   - Function, method, or class names
   - Code snippets or inline code
   - Line numbers
   - Package or library names
   - Commit hashes
4. **Technical direction is OK** — You may describe the approach at a high level: "introduced a caching layer", "reorganized the data validation flow", "added automated testing for the export feature".
5. **Follow the user's language** — Match the language the user has been using in the conversation.

## Source Selection

Decide where to gather information based on the user's context argument:

| Context clue | Source | Action |
|---|---|---|
| Mentions branch, commit, PR, diff, or merge | **Git** | Run `git log` / `git diff` to analyze changes |
| Mentions "just did", "this session", conversation-related phrasing | **Conversation** | Summarize from the current conversation history |
| **No context provided** | **Conversation** | Default to summarizing what the agent did in this conversation |

When using git as the source, determine the appropriate range from context (e.g. current branch vs main, a specific PR, recent N commits).

## Output Format

Choose the format that best fits the content:

- **Few changes** → a short paragraph (2-4 sentences)
- **Multiple distinct changes** → bullet points grouped by theme
- **Mixed** → a one-sentence overview followed by bullets

Always start with a one-line headline that captures the overall intent.

## Prohibited Tools in This Mode

| Tool | Allowed? |
|------|----------|
| Read, Grep, Glob | Yes |
| Bash (read-only: git log, git diff, git status) | Yes |
| Agent (research/explore) | Yes |
| Edit, Write, NotebookEdit | **No** |
| Bash (write/delete/modify) | **No** |

## Examples

### Good

> **新增報表批量匯出功能**
>
> - 支援使用者一次選取多份報表進行匯出，匯出為 ZIP 壓縮檔
> - 加入背景處理機制，避免大量匯出時阻塞畫面
> - 補上匯出失敗時的錯誤提示與重試流程

### Bad (too much engineering detail)

> 在 `ReportController.cs` 新增了 `ExportBatchAsync` method，使用 `ZipArchive` 搭配
> `BackgroundService` 處理，加了 `try-catch` 和 `Polly` retry policy...
